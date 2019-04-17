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
    public sealed class NuGetApexTestService : VisualStudioTestService<NuGetApexVerifier>
    {
        /// <summary>
        /// Gets the NuGet IVsPackageInstallerServices
        /// </summary>
        public IVsPackageInstallerServices InstallerServices => VisualStudioObjectProviders.GetComponentModelService<IVsPackageInstallerServices>();

        /// <summary>
        /// Gets the NuGet IVsPackageInstaller
        /// </summary>
        public IVsPackageInstaller PackageInstaller => VisualStudioObjectProviders.GetComponentModelService<IVsPackageInstaller>();

        /// <summary>
        /// Gets the NuGet IVsSolutionRestoreStatusProvider
        /// </summary>
        public IVsSolutionRestoreStatusProvider SolutionRestoreStatusProvider
            => VisualStudioObjectProviders.GetComponentModelService<IVsSolutionRestoreStatusProvider>();

        public DTE Dte => VisualStudioObjectProviders.DTE;

        /// <summary>
        /// Gets the NuGet IVsPackageUninstaller
        /// </summary>
        public IVsPackageUninstaller PackageUninstaller => VisualStudioObjectProviders.GetComponentModelService<IVsPackageUninstaller>();

        public IVsUIShell UIShell => VisualStudioObjectProviders.GetService<SVsUIShell, IVsUIShell>();

        /// <summary>
        /// Wait for all nominations and auto restore to complete.
        /// This uses an Action to log since the xunit logger is not fully serializable.
        /// </summary>
        public void WaitForAutoRestore()
        {
            var complete = false;

            while (!complete)
            {
#pragma warning disable VSTHRD104 // Offer async methods
                complete = NuGetUIThreadHelper.JoinableTaskFactory.Run(
#pragma warning restore VSTHRD104 // Offer async methods
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
        /// <param name="packageVersion">NuGet package version</param>
        /// <param name="source">Project source</param>
        public void InstallPackage(string projectName, string packageName, string packageVersion = null, string source = null)
        {
            Logger.WriteMessage(
                source == null 
                    ? "Now installing NuGet package [{1} {2}] into project [{3}]"
                    : "Now installing NuGet package [{0} {1} {2}] into project [{3}]",
                source, packageName, packageVersion, projectName);

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
        /// Uninstalls the specified NuGet package from the project
        /// </summary>
        /// <param name="projectName">Project name</param>
        /// <param name="packageName">NuGet package name</param>
        /// <param name="removeDependencies">Whether to uninstall any package dependencies</param>
        public void UninstallPackage(string projectName, string packageName, bool removeDependencies = false)
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
