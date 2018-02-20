// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract,
            typeof(ICrossTargetRuleHandler<DependenciesRuleChangeContext>))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class AssemblyRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "AssemblyDependency";

        protected override string UnresolvedRuleName { get; } = AssemblyReference.SchemaName;
        protected override string ResolvedRuleName { get; } = ResolvedAssemblyReference.SchemaName;
        public override string ProviderType { get; } = ProviderTypeString;

        public override IDependencyModel CreateRootDependencyNode()
        {
            return new SubTreeRootDependencyModel(
                ProviderType,
                VSResources.AssembliesNodeName,
                KnownMonikers.Reference,
                KnownMonikers.ReferenceWarning,
                DependencyTreeFlags.AssemblySubTreeRootNodeFlags);
        }

        protected override IDependencyModel CreateDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new AssemblyDependencyModel(
                providerType,
                path,
                originalItemSpec,
                DependencyTreeFlags.AssemblySubTreeNodeFlags,
                resolved,
                isImplicit,
                properties);
        }

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.ReferencePrivate;
        }
    }
}
