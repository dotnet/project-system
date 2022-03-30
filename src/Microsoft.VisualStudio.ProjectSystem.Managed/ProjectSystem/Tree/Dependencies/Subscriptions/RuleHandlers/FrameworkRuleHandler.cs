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
    internal sealed class FrameworkRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "Framework";

        private static readonly DependencyGroupModel s_groupModel = new(
            ProviderTypeString,
            Resources.FrameworkNodeName,
            new DependencyIconSet(
                icon: KnownMonikers.Framework,
                expandedIcon: KnownMonikers.Framework,
                unresolvedIcon: KnownMonikers.FrameworkWarning,
                unresolvedExpandedIcon: KnownMonikers.FrameworkWarning,
                implicitIcon: KnownMonikers.FrameworkPrivate,
                implicitExpandedIcon: KnownMonikers.FrameworkPrivate),
            DependencyTreeFlags.FrameworkDependencyGroup);

        public FrameworkRuleHandler()
            : base(FrameworkReference.SchemaName, ResolvedFrameworkReference.SchemaName)
        {
        }

        public override string ProviderType => ProviderTypeString;

        public override ProjectTreeFlags GroupNodeFlag => DependencyTreeFlags.FrameworkDependencyGroup;

        public override IDependencyModel CreateRootDependencyNode() => s_groupModel;

        protected override IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new FrameworkDependencyModel(
                path,
                originalItemSpec,
                isResolved,
                isImplicit,
                properties);
        }
    }
}
