// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Represents the restore data for a single target framework in <see cref="UnconfiguredProject"/>.
    /// </summary>
    [DebuggerDisplay("TargetFrameworkMoniker = {TargetFrameworkMoniker}")]
    internal class TargetFrameworkInfo : IVsTargetFrameworkInfo3
    {
        // If additional fields/properties are added to this class, please update RestoreHasher
        public TargetFrameworkInfo(string targetFrameworkMoniker, IVsReferenceItems frameworkReferences, IVsReferenceItems packageDownloads, IVsReferenceItems projectReferences, IVsReferenceItems packageReferences, IVsReferenceItems centralPackageVersions, IVsProjectProperties properties)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            FrameworkReferences = frameworkReferences;
            PackageDownloads = packageDownloads;
            ProjectReferences = projectReferences;
            PackageReferences = packageReferences;
            CentralPackageVersions = centralPackageVersions;
            Properties = properties;
        }

        public string TargetFrameworkMoniker { get; }

        public IVsReferenceItems FrameworkReferences { get; }

        public IVsReferenceItems PackageDownloads { get; }

        public IVsReferenceItems PackageReferences { get; }

        public IVsReferenceItems ProjectReferences { get; }

        public IVsReferenceItems CentralPackageVersions { get; }

        public IVsProjectProperties Properties { get; }
    }
}
