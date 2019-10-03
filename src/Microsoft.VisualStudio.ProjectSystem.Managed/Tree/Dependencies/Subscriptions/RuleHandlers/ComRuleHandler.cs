// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract, typeof(IDependenciesRuleHandler))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class ComRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "ComDependency";

        private static readonly SubTreeRootDependencyModel s_rootModel = new SubTreeRootDependencyModel(
            ProviderTypeString,
            Resources.ComNodeName,
            new DependencyIconSet(
                icon: ManagedImageMonikers.Component,
                expandedIcon: ManagedImageMonikers.Component,
                unresolvedIcon: ManagedImageMonikers.ComponentWarning,
                unresolvedExpandedIcon: ManagedImageMonikers.ComponentWarning));

        public ComRuleHandler()
            : base(ComReference.SchemaName, ResolvedCOMReference.SchemaName)
        {
        }

        public override string ProviderType => ProviderTypeString;

        public override ImageMoniker ImplicitIcon => ManagedImageMonikers.ComponentPrivate;

        public override IDependencyModel CreateRootDependencyNode() => s_rootModel;

        protected override IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new ComDependencyModel(
                path,
                originalItemSpec,
                resolved,
                isImplicit,
                properties);
        }
    }
}
