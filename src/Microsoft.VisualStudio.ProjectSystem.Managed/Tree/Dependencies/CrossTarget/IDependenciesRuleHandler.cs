// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

#nullable enable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore, ContractName = DependencyRulesSubscriber.DependencyRulesSubscriberContract)]
    internal interface IDependenciesRuleHandler
    {
        /// <summary>
        /// Gets the set of rule names this handler handles.
        /// </summary>
        ImmutableHashSet<string> GetRuleNames(RuleHandlerType handlerType);

        /// <summary>
        /// Handles the specified set of changes to a rule, and applies them
        /// to the given <see cref="CrossTargetDependenciesChangesBuilder"/>.
        /// </summary>
        void Handle(
            IImmutableDictionary<string, IProjectChangeDescription> changesByRuleName,
            ITargetFramework targetFramework,
            CrossTargetDependenciesChangesBuilder changesBuilder);
    }
}
