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
        public TargetFrameworkInfo(
            string targetFrameworkMoniker,
            ImmutableArray<ReferenceItem> frameworkReferences,
            ImmutableArray<ReferenceItem> packageDownloads,
            ImmutableArray<ReferenceItem> projectReferences,
            ImmutableArray<ReferenceItem> packageReferences,
            ImmutableArray<ReferenceItem> centralPackageVersions,
            ImmutableArray<ReferenceItem> nuGetAuditSuppress,
            IImmutableDictionary<string, string> properties)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            FrameworkReferences = frameworkReferences;
            PackageDownloads = packageDownloads;
            ProjectReferences = projectReferences;
            PackageReferences = packageReferences;
            CentralPackageVersions = centralPackageVersions;
            NuGetAuditSuppress = nuGetAuditSuppress;
            Properties = properties;
        }

        public string TargetFrameworkMoniker { get; }

        public ImmutableArray<ReferenceItem> FrameworkReferences { get; }

        public ImmutableArray<ReferenceItem> PackageDownloads { get; }

        public ImmutableArray<ReferenceItem> PackageReferences { get; }

        public ImmutableArray<ReferenceItem> ProjectReferences { get; }

        public ImmutableArray<ReferenceItem> CentralPackageVersions { get; }

        public ImmutableArray<ReferenceItem> NuGetAuditSuppress { get; }

        public IImmutableDictionary<string, string> Properties { get; }
    }
}
