// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Contains builder methods for creating <see cref="ProjectRestoreInfo"/> instances.
    /// </summary>
    internal static class RestoreBuilder
    {
        public static readonly ImmutableArray<TargetFrameworkInfo> EmptyTargetFrameworks = ImmutableArray<TargetFrameworkInfo>.Empty;
        public static readonly ImmutableArray<ReferenceItem> EmptyReferences = ImmutableArray<ReferenceItem>.Empty;

        /// <summary>
        ///     Converts an immutable dictionary of rule snapshot data into an <see cref="ProjectRestoreInfo"/> instance.
        /// </summary>
        public static ProjectRestoreInfo ToProjectRestoreInfo(IImmutableDictionary<string, IProjectRuleSnapshot> update)
        {
            IImmutableDictionary<string, string> properties = update.GetSnapshotOrEmpty(NuGetRestore.SchemaName).Properties;
            IProjectRuleSnapshot frameworkReferences = update.GetSnapshotOrEmpty(CollectedFrameworkReference.SchemaName);
            IProjectRuleSnapshot packageDownloads = update.GetSnapshotOrEmpty(CollectedPackageDownload.SchemaName);
            IProjectRuleSnapshot projectReferences = update.GetSnapshotOrEmpty(EvaluatedProjectReference.SchemaName);
            IProjectRuleSnapshot packageReferences = update.GetSnapshotOrEmpty(CollectedPackageReference.SchemaName);
            IProjectRuleSnapshot packageVersions = update.GetSnapshotOrEmpty(CollectedPackageVersion.SchemaName);
            IProjectRuleSnapshot nuGetAuditSuppress = update.GetSnapshotOrEmpty(CollectedNuGetAuditSuppressions.SchemaName);
            IProjectRuleSnapshot toolReferences = update.GetSnapshotOrEmpty(DotNetCliToolReference.SchemaName);

            // For certain project types such as UWP, "TargetFrameworkMoniker" != the moniker that restore uses
            string targetMoniker = properties.GetPropertyOrEmpty(NuGetRestore.NuGetTargetMonikerProperty);
            if (targetMoniker.Length == 0)
                targetMoniker = properties.GetPropertyOrEmpty(NuGetRestore.TargetFrameworkMonikerProperty);

            TargetFrameworkInfo frameworkInfo = new TargetFrameworkInfo(
                targetMoniker,
                ToReferenceItems(frameworkReferences.Items),
                ToReferenceItems(packageDownloads.Items),
                ToReferenceItems(projectReferences.Items),
                ToReferenceItems(packageReferences.Items),
                ToReferenceItems(packageVersions.Items),
                ToReferenceItems(nuGetAuditSuppress.Items),
                properties);

            return new ProjectRestoreInfo(
                properties.GetPropertyOrEmpty(NuGetRestore.MSBuildProjectExtensionsPathProperty),
                properties.GetPropertyOrEmpty(NuGetRestore.ProjectAssetsFileProperty),
                properties.GetPropertyOrEmpty(NuGetRestore.TargetFrameworksProperty),
                EmptyTargetFrameworks.Add(frameworkInfo),
                ToReferenceItems(toolReferences.Items));

            static ImmutableArray<ReferenceItem> ToReferenceItems(IImmutableDictionary<string, IImmutableDictionary<string, string>> items)
            {
                return items.ToImmutableArray(static (key, value) => ToReferenceItem(key, value));

                static ReferenceItem ToReferenceItem(string name, IImmutableDictionary<string, string> metadata)
                {
                    var properties = metadata.ToImmutableArray(static (key, value) => new ReferenceProperty(key, value));

                    return new ReferenceItem(name, metadata);
                }
            }
        }
    }
}
