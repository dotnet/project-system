// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using EnvDTE;
using Microsoft.Test.Apex;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.SolutionRestoreManager;
using NuGet.VisualStudio;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(NuGetApexTestService))]
    public class NuGetApexTestService : VisualStudioTestService<NuGetApexVerifier>
    {
        public NuGetApexTestService()
        {
        }

        /// <summary>
        /// Gets the NuGet IVsPackageInstallerServices
        /// </summary>
        protected internal IVsPackageInstallerServices InstallerServices => VisualStudioObjectProviders.GetComponentModelService<IVsPackageInstallerServices>();

        /// <summary>
        /// Gets the NuGet IVsPackageInstaller
        /// </summary>
        protected internal IVsPackageInstaller PackageInstaller => VisualStudioObjectProviders.GetComponentModelService<IVsPackageInstaller>();

        /// <summary>
        /// Gets the NuGet IVsSolutionRestoreStatusProvider
        /// </summary>
        protected internal IVsSolutionRestoreStatusProvider SolutionRestoreStatusProvider
            => VisualStudioObjectProviders.GetComponentModelService<IVsSolutionRestoreStatusProvider>();

        protected internal DTE Dte => VisualStudioObjectProviders.DTE;

        /// <summary>
        /// Gets the NuGet IVsPackageUninstaller
        /// </summary>
        protected internal IVsPackageUninstaller PackageUninstaller => VisualStudioObjectProviders.GetComponentModelService<IVsPackageUninstaller>();

        protected internal IVsUIShell UIShell => VisualStudioObjectProviders.GetService<SVsUIShell, IVsUIShell>();

        /// <summary>
        /// Wait for all nominations and auto restore to complete.
        /// This uses an Action to log since the xunit logger is not fully serializable.
        /// </summary>
        public void WaitForAutoRestore()
        {
            var complete = false;

            while (!complete)
            {
                complete = NuGetUIThreadHelper.JoinableTaskFactory.Run(
                    () => SolutionRestoreStatusProvider.IsRestoreCompleteAsync(CancellationToken.None));

                if (!complete)
                {
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(300));
                }
            }
        }

        /// <summary>
        /// Installs the specified NuGet package into the specified project
        /// </summary>
        /// <param name="projectName">Project name</param>
        /// <param name="packageName">NuGet package name</param>
        public void InstallPackage(string projectName, string packageName)
        {
            InstallPackage(projectName, packageName, null);
        }

        /// <summary>
        /// Installs the specified NuGet package into the specified project
        /// </summary>
        /// <param name="projectName">Project name</param>
        /// <param name="packageName">NuGet package name</param>
        /// <param name="packageVersion">NuGet package version</param>
        public void InstallPackage(string projectName, string packageName, string packageVersion)
        {
            Logger.WriteMessage("Now installing NuGet package [{0} {1}] into project [{2}]", packageName, packageVersion, packageName);

            InstallPackage(null, projectName, packageName, packageVersion);
        }

        /// <summary>
        /// Installs the specified NuGet package into the specified project
        /// </summary>
        /// <param name="source">Project source</param>
        /// <param name="projectName">Project name</param>
        /// <param name="packageName">NuGet package name</param>
        /// <param name="packageVersion">NuGet package version</param>
        public void InstallPackage(string source, string projectName, string packageName, string packageVersion)
        {
            Logger.WriteMessage("Now installing NuGet package [{0} {1} {2}] into project [{3}]", source, packageName, packageVersion, projectName);

            var project = Dte.Solution.Projects.Item(projectName);

            try
            {
                PackageInstaller.InstallPackage(source, project, packageName, packageVersion, false);
            }
            catch (InvalidOperationException e)
            {
                Logger.WriteException(EntryType.Warning, e, string.Format("An error occured while attempting to install package {0}", packageName));
            }
        }

        /// <summary>
        /// True if the package is installed based on the IVs APIs.
        /// </summary>
        public bool IsPackageInstalled(string projectName, string packageName, string packageVersion)
        {
            var project = Dte.Solution.Projects.Item(projectName);
            return InstallerServices.IsPackageInstalledEx(project, packageName, packageVersion);
        }

        /// <summary>
        /// True if the package is installed based on the IVs APIs.
        /// </summary>
        public bool IsPackageInstalled(string projectName, string packageName)
        {
            var project = Dte.Solution.Projects.Item(projectName);
            return InstallerServices.GetInstalledPackages(project)
                .Any(e => e.Id.Equals(packageName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Uninstalls only the specified NuGet package from the project.
        /// </summary>
        /// <param name="projectName">Project name</param>
        /// <param name="packageName">NuGet package name</param>
        public void UninstallPackage(string projectName, string packageName)
        {
            UninstallPackage(projectName, packageName, false);
        }

        /// <summary>
        /// Uninstalls the specified NuGet package from the project
        /// </summary>
        /// <param name="projectName">Project name</param>
        /// <param name="packageName">NuGet package name</param>
        /// <param name="removeDependencies">Whether to uninstall any package dependencies</param>
        public void UninstallPackage(string projectName, string packageName, bool removeDependencies)
        {
            Logger.WriteMessage("Now uninstalling NuGet package [{0}] from project [{1}]", packageName, projectName);

            var project = Dte.Solution.Projects.Item(projectName);

            try
            {
                PackageUninstaller.UninstallPackage(project, packageName, removeDependencies);
            }
            catch (InvalidOperationException e)
            {
                Logger.WriteException(EntryType.Warning, e, string.Format("An error occured while attempting to uninstall package {0}", packageName));
            }
        }
    }
}
