// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract,
            typeof(ICrossTargetRuleHandler<DependenciesRuleChangeContext>))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class SdkRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "SdkDependency";

        protected override string UnresolvedRuleName { get; } = SdkReference.SchemaName;
        protected override string ResolvedRuleName { get; } = ResolvedSdkReference.SchemaName;
        public override string ProviderType { get; } = ProviderTypeString;

        public override IDependencyModel CreateRootDependencyNode()
        {
            return new SubTreeRootDependencyModel(
                ProviderType,
                VSResources.SdkNodeName,
                ManagedImageMonikers.Sdk,
                ManagedImageMonikers.SdkWarning,
                DependencyTreeFlags.SdkSubTreeRootNodeFlags);
        }

        protected override IDependencyModel CreateDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            // implicit sdk always mark as unresolved, they will be marked resolved when 
            // snapshot filter matches them to corresponding packages
            return new SdkDependencyModel(
                providerType,
                path,
                originalItemSpec,
                DependencyTreeFlags.SdkSubTreeNodeFlags,
                resolved && !isImplicit,
                isImplicit,
                properties);
        }

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.SdkPrivate;
        }
    }
}
