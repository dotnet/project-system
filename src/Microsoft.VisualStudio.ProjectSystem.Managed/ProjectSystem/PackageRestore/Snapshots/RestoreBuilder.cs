// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Contains builder methods for creating <see cref="ProjectRestoreInfo"/> instances.
    /// </summary>
    internal static class RestoreBuilder
    {
        public static readonly ImmutableList<TargetFrameworkInfo> EmptyTargetFrameworks = ImmutableList<TargetFrameworkInfo>.Empty;
        public static readonly ImmutableList<ReferenceItem> EmptyReferences = ImmutableList<ReferenceItem>.Empty;

        /// <summary>
        ///     Converts an immutable dictionary of rule snapshot data into an <see cref="ProjectRestoreInfo"/> instance.
        /// </summary>
        public static ProjectRestoreInfo ToProjectRestoreInfo(IImmutableDictionary<string, IProjectRuleSnapshot> update)
        {
            IImmutableDictionary<string, string> properties = update.GetSnapshotOrEmpty(NuGetRestore.SchemaName).Properties;
            IProjectRuleSnapshot frameworkReferences = update.GetSnapshotOrEmpty(CollectedFrameworkReference.SchemaName);
            IProjectRuleSnapshot packageDownloads = update.GetSnapshotOrEmpty(CollectedPackageDownload.SchemaName);
            IProjectRuleSnapshot projectReferences = update.GetSnapshotOrEmpty(ProjectReference.SchemaName);
            IProjectRuleSnapshot packageReferences = update.GetSnapshotOrEmpty(CollectedPackageReference.SchemaName);
            IProjectRuleSnapshot packageVersions = update.GetSnapshotOrEmpty(CollectedPackageVersion.SchemaName);
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
                ToProjectProperties(properties));

            return new ProjectRestoreInfo(
                properties.GetPropertyOrEmpty(NuGetRestore.MSBuildProjectExtensionsPathProperty),
                properties.GetPropertyOrEmpty(NuGetRestore.ProjectAssetsFileProperty),
                properties.GetPropertyOrEmpty(NuGetRestore.TargetFrameworksProperty),
                EmptyTargetFrameworks.Add(frameworkInfo),
                ToReferenceItems(toolReferences.Items));
        }

        private static ImmutableList<ProjectProperty> ToProjectProperties(IImmutableDictionary<string, string> properties)
        {
            return ImmutableList.CreateRange(properties.Select(x => new ProjectProperty(x.Key, x.Value)));
        }

        private static ImmutableList<ReferenceItem> ToReferenceItems(IImmutableDictionary<string, IImmutableDictionary<string, string>> items)
        {
            return ImmutableList.CreateRange(items.Select(x => ToReferenceItem(x.Key, x.Value)));
        }

        private static ReferenceItem ToReferenceItem(string name, IImmutableDictionary<string, string> metadata)
        {
            return new ReferenceItem(name, ToReferenceProperties(metadata));
        }

        private static ImmutableList<ReferenceProperty> ToReferenceProperties(IImmutableDictionary<string, string> metadata)
        {
            return ImmutableList.CreateRange(metadata.Select(x => new ReferenceProperty(x.Key, x.Value)));
        }
    }
}
