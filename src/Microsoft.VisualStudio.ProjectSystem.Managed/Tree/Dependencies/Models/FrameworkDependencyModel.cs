// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal sealed class FrameworkDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(add: DependencyTreeFlags.FrameworkDependency);

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.Framework,
            expandedIcon: ManagedImageMonikers.Framework,
            unresolvedIcon: ManagedImageMonikers.FrameworkWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.FrameworkWarning);

        public override DependencyIconSet IconSet => s_iconSet;

        public override int Priority => GraphNodePriority.FrameworkReference;

        public override string ProviderType => FrameworkRuleHandler.ProviderTypeString;

        public override string? SchemaItemType => FrameworkReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedFrameworkReference.SchemaName : FrameworkReference.SchemaName;

        public FrameworkDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            IImmutableDictionary<string, string> properties)
            : base(
                path,
                originalItemSpec,
                flags: s_flagCache.Get(isResolved, isImplicit: false),
                isResolved,
                isImplicit: false,
                properties,
                isTopLevel: true)
        {
        }
    }
}
