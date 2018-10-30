// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class AssemblyDependencyModel : DependencyModel
    {
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

        public override string SchemaItemType => AssemblyReference.PrimaryDataSourceItemType;

        public AssemblyDependencyModel(
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(path, originalItemSpec, flags, resolved, isImplicit, properties)
        {
            if (Resolved)
            {
                string fusionName = null;
                Properties?.TryGetValue(ResolvedAssemblyReference.FusionNameProperty, out fusionName);

                Caption = string.IsNullOrEmpty(fusionName) ? Name : new AssemblyName(fusionName).Name;
                SchemaName = ResolvedAssemblyReference.SchemaName;
            }
            else
            {
                Caption = Name;
                SchemaName = AssemblyReference.SchemaName;
            }

            Priority = Dependency.FrameworkAssemblyNodePriority;
        }
    }
}
