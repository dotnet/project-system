// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class ComDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(add: DependencyTreeFlags.ComSubTreeNodeFlags);

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.Component,
            expandedIcon: ManagedImageMonikers.Component,
            unresolvedIcon: ManagedImageMonikers.ComponentWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.ComponentWarning);

        private static readonly DependencyIconSet s_implicitIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.ComponentPrivate,
            expandedIcon: ManagedImageMonikers.ComponentPrivate,
            unresolvedIcon: ManagedImageMonikers.ComponentWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.ComponentWarning);

        public override DependencyIconSet IconSet => Implicit ? s_implicitIconSet : s_iconSet;

        public override string ProviderType => ComRuleHandler.ProviderTypeString;

        public override int Priority => Dependency.ComNodePriority;

        public override string SchemaItemType => ComReference.PrimaryDataSourceItemType;

        public override string SchemaName => Resolved ? ResolvedCOMReference.SchemaName : ComReference.SchemaName;

        public ComDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(
                path,
                originalItemSpec,
                flags: s_flagCache.Get(isResolved, isImplicit),
                isResolved,
                isImplicit,
                properties)
        {
            Caption = isResolved
                ? System.IO.Path.GetFileNameWithoutExtension(path)
                : path;
        }
    }
}
