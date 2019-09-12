// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders
{
    /// <summary>
    /// Handles child, search, and tracking operations for a particular kind of
    /// <see cref="IDependency"/>.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IDependenciesGraphViewProvider
    {
        bool SupportsDependency(IDependency dependency);

        bool HasChildren(IDependency dependency);

        void BuildGraph(
            IGraphContext graphContext,
            string projectPath,
            IDependency dependency,
            GraphNode dependencyGraphNode,
            TargetedDependenciesSnapshot targetedSnapshot);

        /// <summary>
        ///     Gets whether this provider would like to apply changes to a graph node in response to a snapshot update,
        ///     based on the provided arguments.
        /// </summary>
        /// <param name="nodeProjectPath">The project path stored on the graph node that we want to update.</param>
        /// <param name="updatedSnapshotProjectPath">The project path according to the updated snapshot we want to apply changes from.</param>
        /// <param name="updatedDependency">The dependency from the updated snapshot with ID matching the graph node we want to update.</param>
        bool ShouldApplyChanges(string nodeProjectPath, string updatedSnapshotProjectPath, IDependency updatedDependency);

        /// <summary>
        ///     Adds and removes graph nodes to make <paramref name="graphContext"/> match the expected state for
        ///     <paramref name="updatedDependency"/>.
        /// </summary>
        /// <remarks>
        ///     Should only be called if <see cref="ShouldApplyChanges"/> returned <see langword="true" />.
        /// </remarks>
        /// <param name="graphContext">The context via which to make graph changes.</param>
        /// <param name="nodeProjectPath">The path of the project containing the node to update.</param>
        /// <param name="updatedDependency">The dependency that changed, triggering this update. It is a dependency of the project owning the graph node to update.</param>
        /// <param name="dependencyGraphNode">The graph node to update in response to the changed dependency.</param>
        /// <param name="targetedSnapshot">The updated snapshot matching <paramref name="updatedDependency"/>'s target framework.</param>
        /// <returns><see langword="true" /> if the graph context was changed, otherwise <see langword="false" />.</returns>
        bool ApplyChanges(
            IGraphContext graphContext,
            string nodeProjectPath,
            IDependency updatedDependency,
            GraphNode dependencyGraphNode,
            TargetedDependenciesSnapshot targetedSnapshot);

        bool MatchSearchResults(
            IDependency topLevelDependency,
            Dictionary<string, HashSet<IDependency>> searchResultsPerContext,
            [NotNullWhen(returnValue: true)] out HashSet<IDependency>? topLevelDependencyMatches);
    }
}
