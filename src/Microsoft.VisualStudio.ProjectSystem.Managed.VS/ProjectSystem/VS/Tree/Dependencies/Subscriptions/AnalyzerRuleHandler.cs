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
    internal class AnalyzerRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "AnalyzerDependency";

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.CodeInformation,
            expandedIcon: KnownMonikers.CodeInformation,
            unresolvedIcon: ManagedImageMonikers.CodeInformationWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.CodeInformationWarning);

        public AnalyzerRuleHandler()
            : base(AnalyzerReference.SchemaName, ResolvedAnalyzerReference.SchemaName)
        {
        }

        public override string ProviderType => ProviderTypeString;

        public override IDependencyModel CreateRootDependencyNode()
        {
            return new SubTreeRootDependencyModel(
                ProviderTypeString,
                VSResources.AnalyzersNodeName,
                s_iconSet,
                DependencyTreeFlags.AnalyzerSubTreeRootNodeFlags);
        }

        protected override IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new AnalyzerDependencyModel(
                path,
                originalItemSpec,
                DependencyTreeFlags.AnalyzerSubTreeNodeFlags,
                resolved,
                isImplicit,
                properties);
        }

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.CodeInformationPrivate;
        }
    }
}
