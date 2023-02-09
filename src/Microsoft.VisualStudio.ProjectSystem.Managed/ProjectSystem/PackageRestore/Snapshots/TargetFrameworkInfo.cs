// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Represents the restore data for a single target framework in <see cref="UnconfiguredProject"/>.
    /// </summary>
    [DebuggerDisplay("TargetFrameworkMoniker = {TargetFrameworkMoniker}")]
    internal class TargetFrameworkInfo
    {
        // If additional fields/properties are added to this class, please update RestoreHasher
        public TargetFrameworkInfo(string targetFrameworkMoniker, ImmutableList<ReferenceItem> frameworkReferences, ImmutableList<ReferenceItem> packageDownloads, ImmutableList<ReferenceItem> projectReferences, ImmutableList<ReferenceItem> packageReferences, ImmutableList<ReferenceItem> centralPackageVersions, ImmutableList<ProjectProperty> properties)
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

        public ImmutableList<ReferenceItem> FrameworkReferences { get; }

        public ImmutableList<ReferenceItem> PackageDownloads { get; }

        public ImmutableList<ReferenceItem> PackageReferences { get; }

        public ImmutableList<ReferenceItem> ProjectReferences { get; }

        public ImmutableList<ReferenceItem> CentralPackageVersions { get; }

        public ImmutableList<ProjectProperty> Properties { get; }
    }
}
