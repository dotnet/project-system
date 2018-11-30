// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Test.Apex.VisualStudio;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public class NuGetApexVerifier : VisualStudioMarshallableProxyVerifier
    {
        /// <summary>
        /// Gets the Nuget Package Manager test service
        /// </summary>
        private NuGetApexTestService NugetPackageManager => (NuGetApexTestService)Owner;

        /// <summary>
        /// Validate whether a NuGet package is installed
        /// </summary>
        /// <param name="project">project name</param>
        /// <param name="packageName">NuGet package name</param>
        /// <returns>True if the package is installed; otherwise false</returns>
        public bool PackageIsInstalled(string projectName, string packageName)
        {
            var project = NugetPackageManager.Dte.Solution.Projects.Item(projectName);
            return IsTrue(NugetPackageManager.InstallerServices.IsPackageInstalled(project, packageName), "Expected NuGet package {0} to be installed in project {1}.", packageName, project.Name);
        }

        /// <summary>
        /// Validate whether a NuGet package is installed
        /// </summary>
        /// <param name="project">project name</param>
        /// <param name="packageName">NuGet package name</param>
        /// <param name="packageVersion">NuGet package version</param>
        /// <returns>True if the package is installed; otherwise false</returns>
        public bool PackageIsInstalled(string projectName, string packageName, string packageVersion)
        {
            var project = NugetPackageManager.Dte.Solution.Projects.Item(projectName);
            return IsTrue(NugetPackageManager.InstallerServices.IsPackageInstalledEx(project, packageName, packageVersion), "Expected NuGet package {0}-{1} to be installed in project {2}.", packageName, packageVersion, project.Name);
        }

        /// <summary>
        /// Validate whether a NuGet package is not installed
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="packageName">NuGet package name</param>
        /// <returns>True if the package is not installed; otherwise false</returns>
        public bool PackageIsNotInstalled(string projectName, string packageName)
        {
            var project = NugetPackageManager.Dte.Solution.Projects.Item(projectName);
            return IsFalse(NugetPackageManager.InstallerServices.IsPackageInstalled(project, packageName), "Expected NuGet package {0} to not be installed in project {1}.", packageName, project.Name);
        }

        /// <summary>
        /// Validate whether specific version of a NuGet package is not installed
        /// </summary>
        /// <param name="project">Project name</param>
        /// <param name="packageName">NuGet package name</param>
        /// <returns>True if the package is not installed; otherwise false</returns>
        public bool PackageIsNotInstalled(string projectName, string packageName, string packageVersion)
        {
            var project = NugetPackageManager.Dte.Solution.Projects.Item(projectName);
            return IsFalse(NugetPackageManager.InstallerServices.IsPackageInstalledEx(project, packageName, packageVersion), "Expected NuGet package {0}-{1} to not be installed in project {2}.", packageName, packageVersion, project.Name);
        }
    }
}
