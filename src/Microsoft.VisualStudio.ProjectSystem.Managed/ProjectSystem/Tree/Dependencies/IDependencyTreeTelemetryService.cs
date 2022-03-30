// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    /// <summary>
    /// For maintaining light state about dependency tree to generate telemetry
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IDependencyTreeTelemetryService
    {
        /// <summary>
        /// Initialize telemetry state with the set of target frameworks and rules we expect to observe.
        /// </summary>
        void InitializeTargetFrameworkRules(ImmutableArray<TargetFramework> targetFrameworks, IReadOnlyCollection<string> rules);

        /// <summary>
        /// Indicate that a set of rules has been observed in either an Evaluation or Design Time pass.
        /// This information is used when firing tree update telemetry events to indicate whether all rules
        /// have been observed.
        /// </summary>
        void ObserveTargetFrameworkRules(TargetFramework targetFramework, IEnumerable<string> rules);

        /// <summary>
        /// Fire telemetry when dependency tree completes an update
        /// </summary>
        /// <param name="hasUnresolvedDependency">indicates if the snapshot used for the update had any unresolved dependencies</param>
        ValueTask ObserveTreeUpdateCompletedAsync(bool hasUnresolvedDependency);

        /// <summary>
        /// Provides an updated dependency snapshot so that telemetry may be reported about the
        /// state of the project's dependencies.
        /// </summary>
        /// <param name="dependenciesSnapshot">The dependency snapshot.</param>
        void ObserveSnapshot(DependenciesSnapshot dependenciesSnapshot);
    }
}
