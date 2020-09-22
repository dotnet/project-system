// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal class PackageDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(
            resolved: DependencyTreeFlags.PackageDependency + DependencyTreeFlags.SupportsFolderBrowse,
            unresolved: DependencyTreeFlags.PackageDependency);

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.NuGetGrey,
            expandedIcon: ManagedImageMonikers.NuGetGrey,
            unresolvedIcon: ManagedImageMonikers.NuGetGreyWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.NuGetGreyWarning);

        private static readonly DependencyIconSet s_implicitIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.NuGetGreyPrivate,
            expandedIcon: ManagedImageMonikers.NuGetGreyPrivate,
            unresolvedIcon: ManagedImageMonikers.NuGetGreyWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.NuGetGreyWarning);

        public override DependencyIconSet IconSet => Implicit ? s_implicitIconSet : s_iconSet;

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
