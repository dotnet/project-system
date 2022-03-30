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
    internal class AssemblyRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "Assembly";

        private static readonly DependencyGroupModel s_groupModel = new(
            ProviderTypeString,
            Resources.AssembliesNodeName,
            new DependencyIconSet(
                icon: KnownMonikers.Reference,
                expandedIcon: KnownMonikers.Reference,
                unresolvedIcon: KnownMonikers.ReferenceWarning,
                unresolvedExpandedIcon: KnownMonikers.ReferenceWarning,
                implicitIcon: KnownMonikers.ReferencePrivate,
                implicitExpandedIcon: KnownMonikers.ReferencePrivate),
            DependencyTreeFlags.AssemblyDependencyGroup);

        public AssemblyRuleHandler()
            : base(AssemblyReference.SchemaName, ResolvedAssemblyReference.SchemaName)
        {
        }

        public override string ProviderType => ProviderTypeString;

        public override ProjectTreeFlags GroupNodeFlag => DependencyTreeFlags.AssemblyDependencyGroup;

        public override IDependencyModel CreateRootDependencyNode() => s_groupModel;

        protected override IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new AssemblyDependencyModel(
                path,
                originalItemSpec,
                isResolved,
                isImplicit,
                properties);
        }
    }
}
