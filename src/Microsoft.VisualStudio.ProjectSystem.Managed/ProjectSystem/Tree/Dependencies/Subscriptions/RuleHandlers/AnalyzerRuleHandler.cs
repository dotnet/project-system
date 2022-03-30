// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract, typeof(IDependenciesRuleHandler))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class AnalyzerRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "Analyzer";

        private static readonly DependencyGroupModel s_groupModel = new(
            ProviderTypeString,
            Resources.AnalyzersNodeName,
            new DependencyIconSet(
                icon: KnownMonikers.CodeInformation,
                expandedIcon: KnownMonikers.CodeInformation,
                unresolvedIcon: KnownMonikers.CodeInformationWarning,
                unresolvedExpandedIcon: KnownMonikers.CodeInformationWarning,
                implicitIcon: KnownMonikers.CodeInformationPrivate,
                implicitExpandedIcon: KnownMonikers.CodeInformationPrivate),
            DependencyTreeFlags.AnalyzerDependencyGroup);

        public AnalyzerRuleHandler()
            : base(AnalyzerReference.SchemaName, ResolvedAnalyzerReference.SchemaName)
        {
        }

        public override string ProviderType => ProviderTypeString;

        public override ProjectTreeFlags GroupNodeFlag => DependencyTreeFlags.AnalyzerDependencyGroup;

        protected override bool ResolvedItemRequiresEvaluatedItem => false;

        public override IDependencyModel CreateRootDependencyNode() => s_groupModel;

        protected override IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new AnalyzerDependencyModel(
                path,
                originalItemSpec,
                isResolved,
                isImplicit,
                properties);
        }
    }
}
