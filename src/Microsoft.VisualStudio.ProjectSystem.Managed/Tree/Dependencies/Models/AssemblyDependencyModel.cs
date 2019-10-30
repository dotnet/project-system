// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class AssemblyDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(add: DependencyTreeFlags.AssemblyDependency);

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.Reference,
            expandedIcon: KnownMonikers.Reference,
            unresolvedIcon: KnownMonikers.ReferenceWarning,
            unresolvedExpandedIcon: KnownMonikers.ReferenceWarning);

        private static readonly DependencyIconSet s_implicitIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.ReferencePrivate,
            expandedIcon: ManagedImageMonikers.ReferencePrivate,
            unresolvedIcon: KnownMonikers.ReferenceWarning,
            unresolvedExpandedIcon: KnownMonikers.ReferenceWarning);

        public override DependencyIconSet IconSet => Implicit ? s_implicitIconSet : s_iconSet;

        public override string ProviderType => AssemblyRuleHandler.ProviderTypeString;

        public override int Priority => GraphNodePriority.FrameworkAssembly;

        public override string? SchemaItemType => AssemblyReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedAssemblyReference.SchemaName : AssemblyReference.SchemaName;

        public AssemblyDependencyModel(
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
            if (isResolved)
            {
                string? fusionName = Properties.GetStringProperty(ResolvedAssemblyReference.FusionNameProperty);

                Caption = fusionName == null ? path : new AssemblyName(fusionName).Name;
            }
            else
            {
                Caption = path;
            }
        }
    }
}
