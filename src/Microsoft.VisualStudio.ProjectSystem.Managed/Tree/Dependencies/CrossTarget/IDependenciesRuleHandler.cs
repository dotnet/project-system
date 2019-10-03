// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    /// <summary>
    /// Implementations of this interface add, update and remove <see cref="IDependencyModel"/> instances in response to
    /// project rule changes. They use both evaluated items and items returned by targets called during design-time builds.
    /// The latter are fully resolved with all item metadata, while the former contain just the information found in the
    /// project file, which is enough to quickly populate the dependency tree while we wait for the slower design-time
    /// build to complete and return richer item metadata.
    /// </summary>
    [ProjectSystemContract(
        ProjectSystemContractScope.UnconfiguredProject,
        ProjectSystemContractProvider.Private,
        Cardinality = ImportCardinality.ZeroOrMore,
        ContractName = DependencyRulesSubscriber.DependencyRulesSubscriberContract)]
    internal interface IDependenciesRuleHandler
    {
        /// <summary>
        /// Gets the rule name for dependency items returned via evaluation (eg: <c>PackageReference</c>).
        /// </summary>
        string EvaluatedRuleName { get; }

        /// <summary>
        /// Gets the rule name for dependency items resolved by design-time builds (eg: <c>ResolvedPackageReference</c>).
        /// </summary>
        string ResolvedRuleName { get; }

        /// <summary>
        /// Handles the specified set of changes to a rule, and applies them
        /// to the given <see cref="DependenciesChangesBuilder"/>.
        /// </summary>
        void Handle(
            IImmutableDictionary<string, IProjectChangeDescription> changesByRuleName,
            ITargetFramework targetFramework,
            DependenciesChangesBuilder changesBuilder);
    }
}
