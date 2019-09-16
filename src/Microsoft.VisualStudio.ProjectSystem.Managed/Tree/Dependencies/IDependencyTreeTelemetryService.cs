// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// For maintaining light state about dependency tree to generate telemetry
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IDependencyTreeTelemetryService
    {
        /// <summary>
        /// Gets a value indicating whether this telemetry service is active.
        /// If not, then it will remain inactive and no methods need be called on it.
        /// Note that an instance may become inactive during its lifetime.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Initialize telemetry state with the set of target frameworks and rules we expect to observe.
        /// </summary>
        void InitializeTargetFrameworkRules(ImmutableArray<ITargetFramework> targetFrameworks, IReadOnlyCollection<string> rules);

        /// <summary>
        /// Indicate that a set of rules has been observed in either an Evaluation or Design Time pass.
        /// This information is used when firing tree update telemetry events to indicate whether all rules
        /// have been observed.
        /// </summary>
        void ObserveTargetFrameworkRules(ITargetFramework targetFramework, IEnumerable<string> rules);

        /// <summary>
        /// Fire telemetry when dependency tree completes an update
        /// </summary>
        /// <param name="hasUnresolvedDependency">indicates if the snapshot used for the update had any unresolved dependencies</param>
        Task ObserveTreeUpdateCompletedAsync(bool hasUnresolvedDependency);
    }
}
