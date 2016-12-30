// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [Guid(CSharpProjectSystemPackage.XprojTypeGuid)]
    internal sealed class MigrateXprojProjectFactory : FlavoredProjectFactoryBase, IVsProjectUpgradeViaFactory, IVsProjectUpgradeViaFactory4
    {
        private readonly ProcessRunner _runner;
        private readonly IFileSystem _fileSystem;

        public MigrateXprojProjectFactory(ProcessRunner runner, IFileSystem fileSystem)
        {
            Requires.NotNull(runner, nameof(runner));
            Requires.NotNull(fileSystem, nameof(fileSystem));
            _runner = runner;
            _fileSystem = fileSystem;
        }

        public int UpgradeProject(string xprojLocation, uint upgradeFlags, string backupDirectory, out string migratedProjectFileLocation,
            IVsUpgradeLogger logger, out int upgradeRequired, out Guid migratedProjectGuid)
        {
            bool success = false;
            var projectName = Path.GetFileNameWithoutExtension(xprojLocation);
            var hr = UpgradeProject_CheckOnly(xprojLocation, logger, out upgradeRequired, out migratedProjectGuid, out uint dummy);

            // This implementation can only return S_OK. Throw if it returned something else.
            Verify.HResult(hr);

            // First, we back up the project. This function will take care of logging any backup failures.
            if (!BackupProject(backupDirectory, xprojLocation, projectName, logger))
            {
                migratedProjectFileLocation = xprojLocation;
                return VSConstants.VS_E_PROJECTMIGRATIONFAILED;
            }

            var directory = Path.GetDirectoryName(xprojLocation);
            var (logFile, processExitCode) = MigrateProject(directory, xprojLocation, projectName, logger);

            if (!string.IsNullOrEmpty(logFile))
            {
                (migratedProjectFileLocation, success) = LogReport(logFile, processExitCode, projectName, xprojLocation, logger);
            }
            else
            {
                migratedProjectFileLocation = null;
            }

            if (string.IsNullOrEmpty(migratedProjectFileLocation))
            {
                // If we weren't able to find a new csproj, something went very wrong, and dotnet migrate is doing something that we don't expect.
                Assumes.NotNullOrEmpty(migratedProjectFileLocation);
                migratedProjectFileLocation = xprojLocation;
                success = false;
            }

            return success ? VSConstants.S_OK : VSConstants.VS_E_PROJECTMIGRATIONFAILED;
        }

        internal bool BackupProject(string backupLocation, string xprojLocation, string projectName, IVsUpgradeLogger pLogger)
        {
            var directory = Path.GetDirectoryName(xprojLocation);

            // Back up the xproj and project.json to the backup location.
            var xprojName = Path.GetFileName(xprojLocation);
            var backupXprojPath = Path.Combine(backupLocation, xprojName);
            var projectJsonPath = Path.Combine(directory, "project.json");
            var backupProjectJsonPath = Path.Combine(backupLocation, "project.json");

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

            return true;
        }

        internal (string logFile, int exitCode) MigrateProject(string projectDirectory, string xprojLocation, string projectName, IVsUpgradeLogger pLogger)
        {
            var logFile = _fileSystem.GetTempDirectoryOrFileName();

            // We count on dotnet.exe being on the path
            var pInfo = new ProcessStartInfo("dotnet.exe",
                $"migrate --skip-backup -s -x \"{xprojLocation}\" \"{projectDirectory}\" -r \"{logFile}\" --format-report-file-json")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            // First time setup isn't necessary for migration, and causes a long pause with no indication anything is happening.
            // Skip it.
            pInfo.EnvironmentVariables.Add("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true");

            var process = _runner.Start(pInfo);

            // Create strings to hold the output and error text
            var outputBuilder = new StringBuilder();
            var errBuilder = new StringBuilder();

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

            // TODO: we need to read the output from the migration report in addition to console output.
            // We'll still want to read console output in case of a bug in the dotnet cli, and it errors with some exception
            // https://github.com/dotnet/roslyn-project-system/issues/507
            var output = outputBuilder.ToString().Trim();
            var err = errBuilder.ToString().Trim();

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
                    string.Format(VSResources.XprojMigrationFailed, projectName, Path.GetDirectoryName(xprojLocation), xprojLocation, logFile, processExitCode));
                return (string.Empty, false);
            }

            if (!_fileSystem.FileExists(logFile))
            {
                return LogAndReturnError();
            }

            var mainReport = JsonConvert.DeserializeObject<MigrationReport>(_fileSystem.ReadAllText(logFile));
            if (mainReport == null)
            {
                return LogAndReturnError();
            }

            // We're calling migrate on a single project and have don't follow turned on. We shouldn't see any other migration reports.
            Assumes.True(mainReport.ProjectMigrationReports.Count == 1);
            var report = mainReport.ProjectMigrationReports.First();
            if (report.Failed)
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, xprojLocation,
                    string.Format(VSResources.XprojMigrationFailed, projectName, report.ProjectDirectory, xprojLocation, logFile, processExitCode));
            }

            // Note: this format is the same format that is used when dotnet migrate prints to the console. These messages should already be
            // as localized as the cli is, as they come from the cli.
            report.Errors.ForEach(error => pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, xprojLocation,
                $"{error.ErrorCode}::{error.GeneralErrorReason}: {error.Message}"));
            report.Warnings.ForEach(warn => pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_WARNING, projectName, xprojLocation, warn));

            _fileSystem.RemoveFile(logFile);
            return (report.OutputMSBuildProject, report.Succeeded);
        }

        public int UpgradeProject_CheckOnly(string xprojLocation, IVsUpgradeLogger logger, out int upgradeRequired, out Guid migratedProjectFactory, out uint upgradeProjectCapabilityFlags)
        {
            var isXproj = xprojLocation.EndsWith(".xproj");

            // If the project is an xproj, then we need to one-way upgrade it. If it isn't, then there's nothing we can do with it.
            upgradeRequired = isXproj ? (int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_ONEWAYUPGRADE :
                                         (int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_NOREPAIR;

            if (isXproj)
            {
                migratedProjectFactory = new Guid($"{{{CSharpProjectSystemPackage.ProjectTypeGuid}}}");
                upgradeProjectCapabilityFlags = (uint)(__VSPPROJECTUPGRADEVIAFACTORYFLAGS.PUVFF_BACKUPSUPPORTED | __VSPPROJECTUPGRADEVIAFACTORYFLAGS.PUVFF_COPYBACKUP);
            }
            else
            {
                migratedProjectFactory = new Guid(CSharpProjectSystemPackage.XprojTypeGuid);
                upgradeProjectCapabilityFlags = 0;
            }

            return VSConstants.S_OK;
        }

        public void UpgradeProject_CheckOnly(string xprojLocation, IVsUpgradeLogger logger, out uint upgradeRequired, out Guid migratedProjectFactory, out uint upgradeProjectCapabilityFlags)
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
