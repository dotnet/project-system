// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

/// <summary>
///     Represents the restore data for a single target framework in <see cref="UnconfiguredProject"/>.
/// </summary>
[DebuggerDisplay("TargetFrameworkMoniker = {TargetFrameworkMoniker}")]
internal sealed class TargetFrameworkInfo(
    string targetFrameworkMoniker,
    ImmutableArray<ReferenceItem> frameworkReferences,
    ImmutableArray<ReferenceItem> packageDownloads,
    ImmutableArray<ReferenceItem> projectReferences,
    ImmutableArray<ReferenceItem> packageReferences,
    ImmutableArray<ReferenceItem> centralPackageVersions,
    ImmutableArray<ReferenceItem> nuGetAuditSuppress,
    ImmutableArray<ReferenceItem> prunePackageReferences,
    IImmutableDictionary<string, string> properties) : IRestoreState<TargetFrameworkInfo>
{
    // IMPORTANT: If additional state is added, update AddToHash and DescribeChanges below.

    public string TargetFrameworkMoniker { get; } = targetFrameworkMoniker;

    public ImmutableArray<ReferenceItem> FrameworkReferences { get; } = frameworkReferences;

    public ImmutableArray<ReferenceItem> PackageDownloads { get; } = packageDownloads;

    public ImmutableArray<ReferenceItem> PackageReferences { get; } = packageReferences;

    public ImmutableArray<ReferenceItem> ProjectReferences { get; } = projectReferences;

    public ImmutableArray<ReferenceItem> CentralPackageVersions { get; } = centralPackageVersions;

    public ImmutableArray<ReferenceItem> NuGetAuditSuppress { get; } = nuGetAuditSuppress;

    public ImmutableArray<ReferenceItem> PrunePackageReferences { get; } = prunePackageReferences;

    public IImmutableDictionary<string, string> Properties { get; } = properties;

    public void AddToHash(IncrementalHasher hasher)
    {
        hasher.AppendProperty(nameof(TargetFrameworkMoniker), TargetFrameworkMoniker);

        foreach ((string key, string value) in Properties)
        {
            hasher.AppendProperty(key, value);
        }

        hasher.AppendArray(ProjectReferences);
        hasher.AppendArray(PackageReferences);
        hasher.AppendArray(FrameworkReferences);
        hasher.AppendArray(PackageDownloads);
        hasher.AppendArray(CentralPackageVersions);
        hasher.AppendArray(NuGetAuditSuppress);
    }

    public void DescribeChanges(RestoreStateComparisonBuilder builder, TargetFrameworkInfo after)
    {
        builder.PushScope(TargetFrameworkMoniker);

        builder.CompareDictionary(Properties, after.Properties, nameof(Properties));

        builder.CompareArray(ProjectReferences, after.ProjectReferences, nameof(ProjectReferences));
        builder.CompareArray(PackageReferences, after.PackageReferences, nameof(PackageReferences));
        builder.CompareArray(FrameworkReferences, after.FrameworkReferences, nameof(FrameworkReferences));
        builder.CompareArray(PackageDownloads, after.PackageDownloads, nameof(PackageDownloads));
        builder.CompareArray(CentralPackageVersions, after.CentralPackageVersions, nameof(CentralPackageVersions));
        builder.CompareArray(NuGetAuditSuppress, after.NuGetAuditSuppress, nameof(NuGetAuditSuppress));

        builder.PopScope();
    }
}
