// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders
{
    /// <summary>
    /// Handles child, search, and tracking operations for a particular kind of
    /// <see cref="IDependency"/>.
    /// </summary>
    internal interface IDependenciesGraphViewProvider
    {
        bool SupportsDependency(IDependency dependency);

        bool HasChildren(string projectPath, IDependency dependency);

        void BuildGraph(
            IGraphContext graphContext,
            string projectPath,
            IDependency dependency,
            GraphNode dependencyGraphNode,
            ITargetedDependenciesSnapshot targetedSnapshot);

        /// <summary>
        ///     Gets whether this provider would like to apply changes to a graph node in response to a snapshot update,
        ///     based on the provided arguments.
        /// </summary>
        /// <param name="projectPath">The project path stored on the graph node that we want to update.</param>
        /// <param name="updatedProjectPath">The project path according to the updated snapshot we want to apply changes from.</param>
        /// <param name="dependency">The dependency from the updated snapshot with ID matching the graph node we want to update.</param>
        bool ShouldApplyChanges(string projectPath, string updatedProjectPath, IDependency dependency);

        bool ApplyChanges(
            IGraphContext graphContext,
            string projectPath,
            IDependency updatedDependency,
            GraphNode dependencyGraphNode,
            ITargetedDependenciesSnapshot targetedSnapshot);

        bool MatchSearchResults(
            string projectPath,
            IDependency topLevelDependency,
            Dictionary<string, HashSet<IDependency>> searchResultsPerContext,
            out HashSet<IDependency> topLevelDependencyMatches);
    }
}
