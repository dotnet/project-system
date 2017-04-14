// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders
{
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

        bool ShouldTrackChanges(string projectPath, string updatedProjectPath, IDependency dependency);

        bool TrackChanges(
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
