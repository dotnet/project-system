// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        protected override string UnresolvedRuleName { get; } = AnalyzerReference.SchemaName;
        protected override string ResolvedRuleName { get; } = ResolvedAnalyzerReference.SchemaName;
        public override string ProviderType { get; } = ProviderTypeString;

        private readonly IAnalyzerAssemblyService _analyzerAssemblyService;

        [ImportingConstructor]
        public AnalyzerRuleHandler(IAnalyzerAssemblyService analyzerAssemblyService)
        {
            _analyzerAssemblyService = analyzerAssemblyService ?? throw new ArgumentNullException(nameof(analyzerAssemblyService));
        }

        public override IDependencyModel CreateRootDependencyNode()
        {
            return new SubTreeRootDependencyModel(
                ProviderType,
                VSResources.AnalyzersNodeName,
                KnownMonikers.CodeInformation,
                ManagedImageMonikers.CodeInformationWarning,
                DependencyTreeFlags.AnalyzerSubTreeRootNodeFlags);
        }

        protected override IDependencyModel CreateDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            var hasDiagnosticsAnalyzers = _analyzerAssemblyService.ContainsDiagnosticAnalyzers(path);

            var model = new AnalyzerDependencyModel(
                providerType,
                path,
                originalItemSpec,
                DependencyTreeFlags.AnalyzerSubTreeNodeFlags,
                resolved,
                isImplicit,
                hasDiagnosticsAnalyzers,
                properties);

            return model;
        }

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.CodeInformationPrivate;
        }
    }
}
