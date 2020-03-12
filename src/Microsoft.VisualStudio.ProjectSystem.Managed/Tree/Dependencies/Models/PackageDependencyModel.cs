// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class PackageDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(
            add: DependencyTreeFlags.PackageDependency +
                 DependencyTreeFlags.SupportsHierarchy);

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

        public override IImmutableList<string> DependencyIDs { get; }

        public override DependencyIconSet IconSet => Implicit ? s_implicitIconSet : s_iconSet;

        public override string Name { get; }

        public override int Priority => Resolved ? GraphNodePriority.Package : GraphNodePriority.UnresolvedReference;

        public override string ProviderType => PackageRuleHandler.ProviderTypeString;

        public override string? SchemaItemType => PackageReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedPackageReference.SchemaName : PackageReference.SchemaName;

        public PackageDependencyModel(
            string path,
            string originalItemSpec,
            string name,
            string version,
            bool isResolved,
            bool isImplicit,
            bool isTopLevel,
            bool isVisible,
            IImmutableDictionary<string, string> properties,
            IEnumerable<string> dependenciesIDs)
            : base(
                path,
                originalItemSpec,
                flags: s_flagCache.Get(isResolved, isImplicit),
                isResolved,
                isImplicit,
                properties,
                isTopLevel,
                isVisible)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            Name = name;
            Caption = string.IsNullOrEmpty(version) ? name : $"{name} ({version})";

            if (dependenciesIDs != null)
            {
                DependencyIDs = ImmutableArray.CreateRange(dependenciesIDs);
            }
            else
            {
                DependencyIDs = ImmutableList<string>.Empty;
            }
        }
    }
}
