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
    internal class ComRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "Com";

        private static readonly DependencyGroupModel s_groupModel = new(
            ProviderTypeString,
            Resources.ComNodeName,
            new DependencyIconSet(
                icon: KnownMonikers.COM,
                expandedIcon: KnownMonikers.COM,
                unresolvedIcon: KnownMonikers.COMWarning,
                unresolvedExpandedIcon: KnownMonikers.COMWarning,
                implicitIcon: KnownMonikers.COMPrivate,
                implicitExpandedIcon: KnownMonikers.COMPrivate),
            DependencyTreeFlags.ComDependencyGroup);

        public ComRuleHandler()
            : base(ComReference.SchemaName, ResolvedCOMReference.SchemaName)
        {
        }

        public override string ProviderType => ProviderTypeString;

        public override ProjectTreeFlags GroupNodeFlag => DependencyTreeFlags.ComDependencyGroup;

        public override IDependencyModel CreateRootDependencyNode() => s_groupModel;

        protected override IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new ComDependencyModel(
                path,
                originalItemSpec,
                isResolved,
                isImplicit,
                properties);
        }
    }
}
