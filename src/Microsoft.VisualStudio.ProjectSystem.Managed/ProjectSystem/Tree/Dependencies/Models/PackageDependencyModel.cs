// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal class PackageDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new(
            resolved: DependencyTreeFlags.PackageDependency + DependencyTreeFlags.SupportsFolderBrowse,
            unresolved: DependencyTreeFlags.PackageDependency);

        private static readonly DependencyIconSet s_iconSet = new(
            icon: KnownMonikers.NuGetNoColor,
            expandedIcon: KnownMonikers.NuGetNoColor,
            unresolvedIcon: KnownMonikers.NuGetNoColorWarning,
            unresolvedExpandedIcon: KnownMonikers.NuGetNoColorWarning,
            implicitIcon: KnownMonikers.NuGetNoColorPrivate,
            implicitExpandedIcon: KnownMonikers.NuGetNoColorPrivate);

        public override DependencyIconSet IconSet => s_iconSet;

        public override string ProviderType => PackageRuleHandler.ProviderTypeString;

        public override string? SchemaItemType => PackageReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedPackageReference.SchemaName : PackageReference.SchemaName;

        public PackageDependencyModel(
            string originalItemSpec,
            string version,
            bool isResolved,
            bool isImplicit,
            bool isVisible,
            IImmutableDictionary<string, string> properties)
            : base(
                caption: string.IsNullOrEmpty(version) ? originalItemSpec : $"{originalItemSpec} ({version})",
                path: null,
                originalItemSpec,
                flags: s_flagCache.Get(isResolved, isImplicit).Add($"$ID:{originalItemSpec}"),
                isResolved,
                isImplicit,
                properties,
                isVisible)
        {
        }
    }
}
