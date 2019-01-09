// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class PackageDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(
            add: DependencyTreeFlags.NuGetSubTreeNodeFlags +
                 DependencyTreeFlags.PackageNodeFlags +
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

        public override int Priority => Resolved ? Dependency.PackageNodePriority : Dependency.UnresolvedReferenceNodePriority;

        public override string ProviderType => PackageRuleHandler.ProviderTypeString;

        public override string SchemaItemType => PackageReference.PrimaryDataSourceItemType;

        public override string SchemaName => Resolved ? ResolvedPackageReference.SchemaName : PackageReference.SchemaName;

        public override string Version { get; }

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
            Version = version;
            Caption = string.IsNullOrEmpty(version) ? name : $"{name} ({version})";

            if (dependenciesIDs != null)
            {
                DependencyIDs = ImmutableArray.CreateRange(dependenciesIDs);
            }
        }
    }
}
