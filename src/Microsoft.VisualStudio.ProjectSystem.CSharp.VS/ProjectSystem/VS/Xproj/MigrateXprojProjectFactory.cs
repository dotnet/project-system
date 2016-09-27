// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using System;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.Shell.Flavor;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [Guid(CSharpProjectSystemPackage.XprojTypeGuid)]
    internal sealed class MigrateXprojProjectFactory : FlavoredProjectFactoryBase, IVsProjectUpgradeViaFactory, IVsProjectUpgradeViaFactory4
    {
        private readonly ProcessRunner _runner;

        public MigrateXprojProjectFactory(ProcessRunner runner)
        {
            _runner = runner;
        }

        public int UpgradeProject(string bstrFileName, uint fUpgradeFlag, string bstrCopyLocation, out string pbstrUpgradedFullyQualifiedFileName, IVsUpgradeLogger pLogger, out int pUpgradeRequired, out Guid pguidNewProjectFactory)
        {
            bool success;
            uint dummy;
            var projectName = Path.GetFileNameWithoutExtension(bstrFileName);
            var hr = UpgradeProject_CheckOnly(bstrFileName, pLogger, out pUpgradeRequired, out pguidNewProjectFactory, out dummy);

            // This implementation can only return S_OK. Throw if it returned something else.
            Verify.HResult(hr);

            var directory = Path.GetDirectoryName(bstrFileName);

            // Back up the xproj and project.json to the backup location.
            var xprojName = Path.GetFileName(bstrFileName);
            var backupXprojPath = Path.Combine(bstrCopyLocation, xprojName);
            var projectJsonPath = Path.Combine(directory, "project.json");
            var backupProjectJsonPath = Path.Combine(bstrCopyLocation, "project.json");

            if (!File.Exists(projectJsonPath))
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, projectJsonPath,
                    string.Format(VSResources.XprojMigrationFailedProjectJsonFileNotFound, projectName, projectJsonPath));
                pbstrUpgradedFullyQualifiedFileName = bstrFileName;
                return VSConstants.VS_E_PROJECTMIGRATIONFAILED;
            }

            pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, bstrFileName,
                string.Format(VSResources.MigrationBackupFile, bstrFileName, backupXprojPath));
            pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, bstrFileName,
                string.Format(VSResources.MigrationBackupFile, projectJsonPath, backupProjectJsonPath));

            File.Copy(bstrFileName, backupXprojPath, true);
            File.Copy(projectJsonPath, backupProjectJsonPath, true);

            // We count on dotnet.exe being on the path
            var pInfo = new ProcessStartInfo("dotnet.exe", $"migrate -s -p \"{directory}\" -x \"{bstrFileName}\"");
            pInfo.UseShellExecute = false;
            pInfo.RedirectStandardError = true;
            pInfo.RedirectStandardOutput = true;
            var process = _runner.Start(pInfo);
            process.WaitForExit();

            // TODO: we need to read the output from the migration report in addition to console output.
            // We'll still want to read console output in case of a bug in the dotnet cli, and it errors with some exception
            // https://github.com/dotnet/roslyn-project-system/issues/507
            var output = process.StandardOutput.ReadToEnd();
            var err = process.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(output))
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, bstrFileName, output);
            }

            if (!string.IsNullOrEmpty(err))
            {
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_WARNING, projectName, bstrFileName, err);
            }

            if (process.ExitCode == 0)
            {
                success = true;

                // TODO: We need to find the newly created csproj. This will only be necessary until dotnet migrate adds a Migration Report.
                // https://github.com/dotnet/roslyn-project-system/issues/507
                var files = Directory.EnumerateFiles(directory);
                pbstrUpgradedFullyQualifiedFileName = files.FirstOrDefault(file => file.EndsWith(".csproj"));
                if (string.IsNullOrEmpty(pbstrUpgradedFullyQualifiedFileName))
                {
                    pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, bstrFileName, string.Format(VSResources.NoMigratedCSProjFound, directory));
                    pbstrUpgradedFullyQualifiedFileName = bstrFileName;
                    success = false;
                }
            }
            else
            {
                pbstrUpgradedFullyQualifiedFileName = bstrFileName;
                pLogger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, bstrFileName,
                    string.Format(VSResources.XprojMigrationFailed, projectName, directory, bstrFileName, process.ExitCode));
                success = false;
            }

            return success ? VSConstants.S_OK : VSConstants.VS_E_PROJECTMIGRATIONFAILED;
        }

        public int UpgradeProject_CheckOnly(string bstrFileName, IVsUpgradeLogger pLogger, out int pUpgradeRequired, out Guid pguidNewProjectFactory, out uint pUpgradeProjectCapabilityFlags)
        {
            var isXproj = bstrFileName.EndsWith(".xproj");

            // If the project is an xproj, then we need to one-way upgrade it. If it isn't, then there's nothing we can do with it.
            pUpgradeRequired = isXproj ? (int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_ONEWAYUPGRADE :
                                         (int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_NOREPAIR;

            if (isXproj)
            {
                pguidNewProjectFactory = new Guid($"{{{CSharpProjectSystemPackage.ProjectTypeGuid}}}");
                pUpgradeProjectCapabilityFlags = (uint)(__VSPPROJECTUPGRADEVIAFACTORYFLAGS.PUVFF_BACKUPSUPPORTED | __VSPPROJECTUPGRADEVIAFACTORYFLAGS.PUVFF_COPYBACKUP);
            }
            else
            {
                pguidNewProjectFactory = new Guid(CSharpProjectSystemPackage.XprojTypeGuid);
                pUpgradeProjectCapabilityFlags = 0;
            }

            return VSConstants.S_OK;
        }

        public void UpgradeProject_CheckOnly(string pszFileName, IVsUpgradeLogger pLogger, out uint pUpgradeRequired, out Guid pguidNewProjectFactory, out uint pUpgradeProjectCapabilityFlags)
        {
            int upgradeRequired;
            UpgradeProject_CheckOnly(pszFileName, pLogger, out upgradeRequired, out pguidNewProjectFactory, out pUpgradeProjectCapabilityFlags);
            pUpgradeRequired = unchecked((uint)upgradeRequired);
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
