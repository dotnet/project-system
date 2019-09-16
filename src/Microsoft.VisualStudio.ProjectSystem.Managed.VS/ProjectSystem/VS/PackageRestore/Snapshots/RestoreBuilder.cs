// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Contains builder methods for creating <see cref="IVsProjectRestoreInfo"/> instances.
    /// </summary>
    internal static class RestoreBuilder
    {
        /// <summary>
        ///     Converts an immutable dictionary of rule snapshot data into an <see cref="IVsProjectRestoreInfo"/> instance.
        /// </summary>
        public static ProjectRestoreInfo ToProjectRestoreInfo(IImmutableDictionary<string, IProjectRuleSnapshot> update)
        {
            IImmutableDictionary<string, string> properties = update.GetSnapshotOrEmpty(NuGetRestore.SchemaName).Properties;
            IProjectRuleSnapshot frameworkReferences = update.GetSnapshotOrEmpty(CollectedFrameworkReference.SchemaName);
            IProjectRuleSnapshot packageDownloads = update.GetSnapshotOrEmpty(CollectedPackageDownload.SchemaName);
            IProjectRuleSnapshot projectReferences = update.GetSnapshotOrEmpty(ProjectReference.SchemaName);
            IProjectRuleSnapshot packageReferences = update.GetSnapshotOrEmpty(PackageReference.SchemaName);
            IProjectRuleSnapshot toolReferences = update.GetSnapshotOrEmpty(DotNetCliToolReference.SchemaName);

            // For certain project types such as UWP, "TargetFrameworkMoniker" != the moniker that restore uses
            string targetMoniker = properties.GetPropertyOrEmpty(NuGetRestore.NuGetTargetMonikerProperty);
            if (targetMoniker.Length == 0)
                targetMoniker = properties.GetPropertyOrEmpty(NuGetRestore.TargetFrameworkMonikerProperty);

            IVsTargetFrameworkInfo2 frameworkInfo = new TargetFrameworkInfo(
                targetMoniker,
                ToReferenceItems(frameworkReferences.Items),
                ToReferenceItems(packageDownloads.Items),
                ToReferenceItems(projectReferences.Items),
                ToReferenceItems(packageReferences.Items),
                ToProjectProperties(properties));

            return new ProjectRestoreInfo(
                properties.GetPropertyOrEmpty(NuGetRestore.MSBuildProjectExtensionsPathProperty),
                properties.GetPropertyOrEmpty(NuGetRestore.ProjectAssetsFileProperty),
                properties.GetPropertyOrEmpty(NuGetRestore.TargetFrameworksProperty),
                new TargetFrameworks(new[] { frameworkInfo }),
                ToReferenceItems(toolReferences.Items));
        }

        private static IVsProjectProperties ToProjectProperties(IImmutableDictionary<string, string> properties)
        {
            return new ProjectProperties(properties.Select(v => new ProjectProperty(v.Key, v.Value)));
        }

        private static IVsReferenceItems ToReferenceItems(IImmutableDictionary<string, IImmutableDictionary<string, string>> items)
        {
            return new ReferenceItems(items.Select(item => ToReferenceItem(item.Key, item.Value)));
        }

        private static IVsReferenceItem ToReferenceItem(string name, IImmutableDictionary<string, string> metadata)
        {
            return new ReferenceItem(name, ToReferenceProperties(metadata));
        }

        private static IVsReferenceProperties ToReferenceProperties(IImmutableDictionary<string, string> metadata)
        {
            return new ReferenceProperties(metadata.Select(property => new ReferenceProperty(property.Key, property.Value)));
        }
    }
}
