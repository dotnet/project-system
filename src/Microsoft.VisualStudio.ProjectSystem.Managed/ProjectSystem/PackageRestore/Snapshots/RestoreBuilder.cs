// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

/// <summary>
///     Contains builder methods for creating <see cref="ProjectRestoreInfo"/> instances.
/// </summary>
internal static class RestoreBuilder
{
    public static readonly ImmutableArray<TargetFrameworkInfo> EmptyTargetFrameworks = [];
    public static readonly ImmutableArray<ReferenceItem> EmptyReferences = [];

    /// <summary>
    ///     Converts an immutable dictionary of rule snapshot data into an <see cref="ProjectRestoreInfo"/> instance.
    /// </summary>
    public static ProjectRestoreInfo ToProjectRestoreInfo(IImmutableDictionary<string, IProjectRuleSnapshot> update)
    {
        IImmutableDictionary<string, string> properties = update.GetSnapshotOrEmpty(NuGetRestore.SchemaName).Properties;

        // For certain project types such as UWP, "TargetFrameworkMoniker" != the moniker that restore uses
        string targetMoniker = properties.GetPropertyOrEmpty(NuGetRestore.NuGetTargetMonikerProperty);
        if (targetMoniker.Length == 0)
            targetMoniker = properties.GetPropertyOrEmpty(NuGetRestore.TargetFrameworkMonikerProperty);

        TargetFrameworkInfo frameworkInfo = new(
            targetMoniker,
            frameworkReferences: GetReferenceItems(CollectedFrameworkReference.SchemaName),
            packageDownloads: GetReferenceItems(CollectedPackageDownload.SchemaName),
            projectReferences: GetReferenceItems(EvaluatedProjectReference.SchemaName),
            packageReferences: GetReferenceItems(CollectedPackageReference.SchemaName),
            centralPackageVersions: GetReferenceItems(CollectedPackageVersion.SchemaName),
            nuGetAuditSuppress: GetReferenceItems(CollectedNuGetAuditSuppressions.SchemaName),
            prunePackageReferences: GetReferenceItems(CollectedPrunePackageReference.SchemaName),
            properties: properties);

        return new ProjectRestoreInfo(
            msbuildProjectExtensionsPath: properties.GetPropertyOrEmpty(NuGetRestore.MSBuildProjectExtensionsPathProperty),
            projectAssetsFilePath: properties.GetPropertyOrEmpty(NuGetRestore.ProjectAssetsFileProperty),
            originalTargetFrameworks: properties.GetPropertyOrEmpty(NuGetRestore.TargetFrameworksProperty),
            targetFrameworks: [frameworkInfo],
            toolReferences: GetReferenceItems(DotNetCliToolReference.SchemaName));

        ImmutableArray<ReferenceItem> GetReferenceItems(string schemaName)
        {
            if (!update.TryGetValue(schemaName, out IProjectRuleSnapshot? result))
            {
                return [];
            }

            return result.Items.ToImmutableArray(static (name, metadata) => new ReferenceItem(name, metadata));
        }
    }
}
