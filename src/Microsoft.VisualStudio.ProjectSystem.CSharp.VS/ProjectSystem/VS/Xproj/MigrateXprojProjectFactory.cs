// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [Guid("8bb2217d-0f2d-49d1-97bc-3654ed321f3b")]
    internal sealed class MigrateXprojProjectFactory : FlavoredProjectFactoryBase, IVsProjectUpgradeViaFactory, IVsProjectUpgradeViaFactory4
    {
        private readonly ProcessRunner _runner;
        private readonly IFileSystem _fileSystem;
        private readonly IServiceProvider _serviceProvider;
        private readonly GlobalJsonRemover.GlobalJsonSetup _globalJsonSetup;

        public MigrateXprojProjectFactory(ProcessRunner runner,
            IFileSystem fileSystem,
            IServiceProvider serviceProvider,
            GlobalJsonRemover.GlobalJsonSetup globalJsonSetup)
        {
            Requires.NotNull(runner, nameof(runner));
            Requires.NotNull(fileSystem, nameof(fileSystem));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(globalJsonSetup, nameof(globalJsonSetup));
            _runner = runner;
            _fileSystem = fileSystem;
            _serviceProvider = serviceProvider;
            _globalJsonSetup = globalJsonSetup;
        }

        public int UpgradeProject(string xprojLocation, uint upgradeFlags, string backupDirectory, out string migratedProjectFileLocation,
            IVsUpgradeLogger logger, out int upgradeRequired, out Guid migratedProjectGuid)
        {
            UIThreadHelper.VerifyOnUIThread();
            bool success = false;
            string projectName = Path.GetFileNameWithoutExtension(xprojLocation);
            int hr = UpgradeProject_CheckOnly(xprojLocation, logger, out upgradeRequired, out migratedProjectGuid, out _);

            // This implementation can only return S_OK. Throw if it returned something else.
            Verify.HResult(hr);

            // First, we back up the project. This function will take care of logging any backup failures.
            if (!BackupProject(backupDirectory, xprojLocation, projectName, logger))
            {
                migratedProjectFileLocation = xprojLocation;
                return VSConstants.VS_E_PROJECTMIGRATIONFAILED;
            }

            IVsSolution solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
            Verify.HResult(solution.GetSolutionInfo(out string solutionDirectory, out _, out _));

            HResult backupResult = BackupAndDeleteGlobalJson(solutionDirectory, solution, backupDirectory, projectName, logger);
            if (!backupResult.Succeeded)
            {
                migratedProjectGuid = GetType().GUID;
                migratedProjectFileLocation = xprojLocation;
                return backupResult;
            }

            string directory = Path.GetDirectoryName(xprojLocation);
            (string logFile, int processExitCode) = MigrateProject(solutionDirectory, directory, xprojLocation, projectName, logger);

            if (!string.IsNullOrEmpty(logFile))
            {
                (migratedProjectFileLocation, success) = LogReport(logFile, processExitCode, projectName, xprojLocation, logger);
            }
            else
            {
                migratedProjectGuid = GetType().GUID;
                migratedProjectFileLocation = null;
            }

            if (string.IsNullOrEmpty(migratedProjectFileLocation))
            {
                // If we weren't able to find a new csproj, something went very wrong, and dotnet migrate is doing something that we don't expect.
                migratedProjectGuid = GetType().GUID;
                Assumes.NotNullOrEmpty(migratedProjectFileLocation);
                migratedProjectFileLocation = xprojLocation;
                success = false;
            }

            if (success)
            {
                CleanupXproj(directory, projectName);
            }

            return success ? VSConstants.S_OK : VSConstants.VS_E_PROJECTMIGRATIONFAILED;
        }

        internal bool BackupProject(string backupLocation, string xprojLocation, string projectName, IVsUpgradeLogger pLogger)
        {
            string directory = Path.GetDirectoryName(xprojLocation);

            // Back up the xproj and project.json to the backup location. If it exists, also back up the .xproj.user file.
            string xprojName = Path.GetFileName(xprojLocation);
            string backupXprojPath = Path.Combine(backupLocation, xprojName);
            string projectJsonPath = Path.Combine(directory, "project.json");
            string backupProjectJsonPath = Path.Combine(backupLocation, "project.json");
            string xprojUserPath = $"{xprojLocation}.user";
            string backupXprojUserPath = Path.Combine(backupLocation, $"{xprojName}.user");

            // We don't need to check the xproj path. That's being given to us by VS and was specified in the solution.
            if (!_fileSystem.FileExists(projectJsonPath))
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, projectJsonPath,
                    string.Format(VSResources.XprojMigrationFailedProjectJsonFileNotFound, projectName, projectJsonPath));
                return false;
            }

            pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, xprojLocation,
                string.Format(VSResources.MigrationBackupFile, xprojLocation, backupXprojPath));
            pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, projectJsonPath,
                string.Format(VSResources.MigrationBackupFile, projectJsonPath, backupProjectJsonPath));

            _fileSystem.CopyFile(xprojLocation, backupXprojPath, true);
            _fileSystem.CopyFile(projectJsonPath, backupProjectJsonPath, true);

            if (_fileSystem.FileExists(xprojUserPath))
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, xprojUserPath,
                    string.Format(VSResources.MigrationBackupFile, xprojUserPath, backupXprojUserPath));
                _fileSystem.CopyFile(xprojUserPath, backupXprojUserPath, true);
            }

            return true;
        }

        internal HResult BackupAndDeleteGlobalJson(string solutionDirectory, IVsSolution solution, string backupLocation, string projectName, IVsUpgradeLogger pLogger)
        {
            string globalJson = Path.Combine(solutionDirectory, "global.json");
            if (_fileSystem.FileExists(globalJson))
            {
                // We want to set up the remover if it hasn't been set up already. If it has been set up already, then we can just return, as backup
                // and parsing for the sdk element will already have occurred.
                if (!_globalJsonSetup.SetupRemoval(solution, _serviceProvider, _fileSystem))
                    return HResult.OK;

                // We want to find the root backup directory that VS created for backup. We can't just assume it's solution/Backup, because VS will create
                // a new directory if that already exists. So just iterate up until we find the correct directory.
                solutionDirectory = solutionDirectory.TrimEnd(Path.DirectorySeparatorChar);
                string rootBackupDirectory = backupLocation;
                string levelUpBackupDirectory = Path.GetDirectoryName(rootBackupDirectory).TrimEnd(Path.DirectorySeparatorChar);
                while (!StringComparers.Paths.Equals(levelUpBackupDirectory, solutionDirectory))
                {
                    rootBackupDirectory = levelUpBackupDirectory;
                    levelUpBackupDirectory = Path.GetDirectoryName(levelUpBackupDirectory).TrimEnd(Path.DirectorySeparatorChar);
                }

                // rootBackupDirectory is now the actual root backup directory. Back up the global.json and delete the existing one
                string globalJsonBackupPath = Path.Combine(rootBackupDirectory, "global.json");
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, globalJson,
                    string.Format(VSResources.MigrationBackupFile, globalJson, globalJsonBackupPath));
                _fileSystem.CopyFile(globalJson, globalJsonBackupPath, true);

                // Now parse the global.json, and remove the "sdk" element if it exists
                JObject json = JsonConvert.DeserializeObject<JObject>(_fileSystem.ReadAllText(globalJson));
                if (json.Remove("sdk"))
                {
                    try
                    {
                        _fileSystem.WriteAllText(globalJson, JsonConvert.SerializeObject(json));
                    }
                    catch (IOException ex)
                    {
                        pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, globalJson, ex.Message);
                        return ex.HResult;
                    }
                }
            }
            return VSConstants.S_OK;
        }

        internal (string logFile, int exitCode) MigrateProject(string solutionDirectory, string projectDirectory, string xprojLocation, string projectName, IVsUpgradeLogger pLogger)
        {
            string logFile = _fileSystem.GetTempDirectoryOrFileName();

            // We count on dotnet.exe being on the path
            var pInfo = new ProcessStartInfo("dotnet.exe", GetDotnetArguments(xprojLocation, projectDirectory, logFile))
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = solutionDirectory
            };

            // First time setup isn't necessary for migration, and causes a long pause with no indication anything is happening.
            // Skip it.
            pInfo.EnvironmentVariables.Add("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true");

            ProcessWrapper process = _runner.Start(pInfo);

            // Create strings to hold the output and error text
            var outputBuilder = PooledStringBuilder.GetInstance();
            var errBuilder = PooledStringBuilder.GetInstance();

            process.AddOutputDataReceivedHandler(o =>
            {
                outputBuilder.AppendLine(o);
            });

            process.AddErrorDataReceivedHandler(e =>
            {
                errBuilder.AppendLine(e);
            });

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();

            string output = outputBuilder.ToStringAndFree().Trim();
            string err = errBuilder.ToStringAndFree().Trim();

            if (!string.IsNullOrEmpty(output))
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, xprojLocation, output);
            }

            if (!string.IsNullOrEmpty(err))
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_WARNING, projectName, xprojLocation, err);
            }

            return (logFile, process.ExitCode);
        }

        internal (string projFile, bool success) LogReport(string logFile, int processExitCode,
            string projectName, string xprojLocation, IVsUpgradeLogger pLogger)
        {
            (string, bool) LogAndReturnError()
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, xprojLocation,
                    string.Format(VSResources.XprojMigrationFailedCannotReadReport, logFile));
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, xprojLocation,
                    GetDotnetGeneralErrorString(projectName, xprojLocation, Path.GetDirectoryName(xprojLocation), logFile, processExitCode));
                return (string.Empty, false);
            }

            if (!_fileSystem.FileExists(logFile))
            {
                return LogAndReturnError();
            }

            MigrationReport mainReport = JsonConvert.DeserializeObject<MigrationReport>(_fileSystem.ReadAllText(logFile));
            if (mainReport == null)
            {
                return LogAndReturnError();
            }

            // We're calling migrate on a single project and have don't follow turned on. We shouldn't see any other migration reports.
            Assumes.True(mainReport.ProjectMigrationReports.Count == 1);
            ProjectMigrationReport report = mainReport.ProjectMigrationReports[0];
            if (report.Failed)
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, xprojLocation,
                    GetDotnetGeneralErrorString(projectName, xprojLocation, report.ProjectDirectory, logFile, processExitCode));
            }

            foreach (MigrationError error in report.Errors)
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, xprojLocation, error.FormattedErrorMessage);
            }

            foreach (string warn in report.Warnings)
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_WARNING, projectName, xprojLocation, warn);
            }

            _fileSystem.RemoveFile(logFile);
            return (report.OutputMSBuildProject, report.Succeeded);
        }

        internal void CleanupXproj(string projectLocation, string projectName)
        {
            // Clean up the .xproj, the project.json, the project.lock.json, and the xproj.user
            _fileSystem.RemoveFile(Path.Combine(projectLocation, $"{projectName}.xproj"));
            try
            {
                _fileSystem.RemoveFile(Path.Combine(projectLocation, $"{projectName}.xproj.user"));
            }
            catch (FileNotFoundException) { }
            _fileSystem.RemoveFile(Path.Combine(projectLocation, "project.json"));
            try
            {
                _fileSystem.RemoveFile(Path.Combine(projectLocation, "project.lock.json"));
            }
            catch (FileNotFoundException) { }
        }

        public int UpgradeProject_CheckOnly(string xprojLocation,
            IVsUpgradeLogger logger,
            out int upgradeRequired,
            out Guid migratedProjectFactory,
            out uint upgradeProjectCapabilityFlags)
        {
            bool isXproj = xprojLocation.EndsWith(".xproj");

            // If the project is an xproj, then we need to one-way upgrade it. If it isn't, then there's nothing we can do with it.
            upgradeRequired = isXproj ? (int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_ONEWAYUPGRADE :
                                         (int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_NOREPAIR;

            if (isXproj)
            {
                migratedProjectFactory = new Guid(CSharpProjectSystemPackage.ProjectTypeGuid);
                upgradeProjectCapabilityFlags = (uint)(__VSPPROJECTUPGRADEVIAFACTORYFLAGS.PUVFF_BACKUPSUPPORTED | __VSPPROJECTUPGRADEVIAFACTORYFLAGS.PUVFF_COPYBACKUP);
            }
            else
            {
                migratedProjectFactory = GetType().GUID;
                upgradeProjectCapabilityFlags = 0;
            }

            return VSConstants.S_OK;
        }

        private static string GetDotnetArguments(string xprojLocation, string projectDirectory, string logFile) =>
            $"migrate --skip-backup -s -x \"{xprojLocation}\" \"{projectDirectory}\\project.json\" -r \"{logFile}\" --format-report-file-json";

        private static string GetDotnetGeneralErrorString(string projectName, string xprojLocation, string projectDirectory, string logFile, int exitCode) =>
            string.Format(VSResources.XprojMigrationGeneralFailure,
                projectName,
                $"dotnet {GetDotnetArguments(xprojLocation, projectDirectory, logFile)}",
                exitCode);

        public void UpgradeProject_CheckOnly(string xprojLocation,
            IVsUpgradeLogger logger,
            out uint upgradeRequired,
            out Guid migratedProjectFactory,
            out uint upgradeProjectCapabilityFlags)
        {
            UpgradeProject_CheckOnly(xprojLocation, logger, out int iUpgradeRequired, out migratedProjectFactory, out upgradeProjectCapabilityFlags);
            upgradeRequired = unchecked((uint)iUpgradeRequired);
        }

        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            // We're migrated before we get here
            throw new NotImplementedException();
        }

        public int GetSccInfo(string bstrProjectFileName, out string pbstrSccProjectName, out string pbstrSccAuxPath, out string pbstrSccLocalPath, out string pbstrProvider)
        {
            // We're migrated before we get here
            throw new NotImplementedException();
        }
    }
}
