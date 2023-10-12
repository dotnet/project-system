// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies
{
    [Export(typeof(IMSBuildDependencyFactory))]
    [AppliesTo(ProjectCapability.DependenciesTree + " & " + ProjectCapabilities.PackageReferences)]
    internal sealed class PackageDependencyFactory : MSBuildDependencyFactoryBase
    {
        private static readonly DependencyFlagCache s_flagCache = new(
            resolved: DependencyTreeFlags.PackageDependency + DependencyTreeFlags.SupportsFolderBrowse,
            unresolved: DependencyTreeFlags.PackageDependency);

        // Resolved package reference items (via the _PackageDependenciesDesignTime target) have a
        // composite item spec of form name/version, such as "MyPackage/1.2.3".
        //
        // The original (unresolved) name is available via the "Name" metadata.
        //
        //     MyPackage/1.2.3
        //         Resolved = True
        //         IsImplicitlyDefined = False
        //         Version = 1.2.3
        //         Name = MyPackage
        //         Path = C:\Users\drnoakes\.nuget\packages\mypackage\1.2.3
        //
        // Prior to 16.7 (SDK 3.1.4xx) an additional "Type" metadatum was present.

        public override DependencyGroupType DependencyGroupType => DependencyGroupTypes.Packages;

        public override string UnresolvedRuleName => PackageReference.SchemaName;
        public override string ResolvedRuleName => ResolvedPackageReference.SchemaName;

        public override string SchemaItemType => PackageReference.PrimaryDataSourceItemType;

        public override ProjectImageMoniker Icon => KnownProjectImageMonikers.NuGetNoColor;
        public override ProjectImageMoniker IconWarning => KnownProjectImageMonikers.NuGetNoColorWarning;
        public override ProjectImageMoniker IconError => KnownProjectImageMonikers.NuGetNoColorError;
        public override ProjectImageMoniker IconImplicit => KnownProjectImageMonikers.NuGetNoColorPrivate;

        public override DependencyFlagCache FlagCache => s_flagCache;

        protected internal override string GetUnresolvedCaption(string itemSpec, IImmutableDictionary<string, string> unresolvedProperties)
        {
            string? version = unresolvedProperties.GetStringProperty(ProjectItemMetadata.Version);

            return string.IsNullOrWhiteSpace(version) ? itemSpec : $"{itemSpec} ({version})";
        }

        protected internal override string GetResolvedCaption(string itemSpec, string? originalItemSpec, IImmutableDictionary<string, string> resolvedProperties)
        {
            string? version = resolvedProperties.GetStringProperty(ProjectItemMetadata.Version);

            return string.IsNullOrWhiteSpace(version) ? originalItemSpec ?? itemSpec : $"{originalItemSpec} ({version})";
        }

        protected internal override string? GetOriginalItemSpec(string resolvedItemSpec, IImmutableDictionary<string, string> resolvedProperties)
        {
            // We have design-time build data
            string? dependencyType = resolvedProperties.GetStringProperty(ProjectItemMetadata.Type);

            if (dependencyType is not null)
            {
                // In 16.7 (SDK 3.1.4xx) the format of ResolvedPackageReference items was changed in task PreprocessPackageDependenciesDesignTime.
                //
                // If we observe "Type" metadata then we are running with an older SDK, which is out of support.
                //
                // Legacy behavior had a few differences:
                //
                // - Both direct and transitive dependencies were included.
                // - Packages for all targets were returned, even though we have a build per-target.
                // - The package's ItemSpec included its target (for example: ".NETFramework,Version=v4.8/MyPackage/1.2.3").
                //
                // From 16.7 to 17.5 we would attempt to parse the results of the outdated SDK. From 17.6, if we detect data
                // of this format, we are no longer able to parse it and exclude these items. The 3.1 SDK went out of support before
                // 17.5 was released.

                // Return null such that this item can not be considered for further processing, as it cannot
                // be matched with an evaluated item without the original item spec.
                return null;
            }

            // Resolved package references have the version in the item spec (i.e. "MyPackage/1.2.3"),
            // so we dig into the "Name" metadata to obtain the original item spec.
            //
            // This should always be present, as Name is required in PreprocessPackageDependenciesDesignTime from 16.7.
            return resolvedProperties.GetStringProperty(ProjectItemMetadata.Name);
        }

        protected internal override ProjectTreeFlags UpdateTreeFlags(string id, ProjectTreeFlags flags)
        {
            return flags.Add($"$ID:{id}");
        }
    }
}
