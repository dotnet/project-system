// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    /// <summary>
    /// Updates the graph to include any <see cref="GraphNode"/>s matching the search criteria.
    /// </summary>
    [Export(typeof(IDependenciesGraphActionHandler))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class SearchGraphActionHandler : GraphActionHandlerBase
    {
        public const int Order = 120;

        private readonly IDependenciesGraphBuilder _builder;

        [ImportingConstructor]
        public SearchGraphActionHandler(
            IDependenciesGraphBuilder builder,
            IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(aggregateSnapshotProvider)
        {
            _builder = builder;
        }

        public override bool TryHandleRequest(IGraphContext graphContext)
        {
            if (graphContext.Direction == GraphContextDirection.Custom)
            {
                Search(graphContext);
            }

            return false;
        }

        /// <summary>
        /// Generates search graph containing nodes matching search criteria in Solution Explorer 
        /// and attaches it to correct top level node.
        /// </summary>
        private void Search(IGraphContext graphContext)
        {
            string searchParametersTypeName = typeof(ISolutionSearchParameters).GUID.ToString();
            ISolutionSearchParameters searchParameters = graphContext.GetValue<ISolutionSearchParameters>(searchParametersTypeName);

            string? searchTerm = searchParameters?.SearchQuery.SearchString;
            if (searchTerm == null)
            {
                return;
            }

            var cachedDependencyToMatchingResultsMap = new Dictionary<string, HashSet<IDependency>>(StringComparers.DependencyTreeIds);
            var searchResultsPerContext = new Dictionary<string, HashSet<IDependency>>(StringComparers.Paths);

            System.Collections.Generic.IReadOnlyCollection<DependenciesSnapshot> snapshots = AggregateSnapshotProvider.GetSnapshots();

            foreach (DependenciesSnapshot snapshot in snapshots)
            {
                searchResultsPerContext[snapshot.ProjectPath] = SearchFlat(searchTerm, snapshot);
            }

            foreach (DependenciesSnapshot snapshot in snapshots)
            {
                IEnumerable<IDependency> allTopLevelDependencies = snapshot.GetFlatTopLevelDependencies();
                HashSet<IDependency> matchedDependencies = searchResultsPerContext[snapshot.ProjectPath];

                using var scope = new GraphTransactionScope();

                foreach (IDependency topLevelDependency in allTopLevelDependencies)
                {
                    TargetedDependenciesSnapshot targetedSnapshot = snapshot.DependenciesByTargetFramework[topLevelDependency.TargetFramework];

                    if (!cachedDependencyToMatchingResultsMap.TryGetValue(
                        topLevelDependency.Id,
                        out HashSet<IDependency>? topLevelDependencyMatches))
                    {
                        IDependenciesGraphViewProvider? viewProvider = FindViewProvider(topLevelDependency);

                        if (viewProvider == null)
                        {
                            continue;
                        }

                        if (!viewProvider.MatchSearchResults(
                            topLevelDependency,
                            searchResultsPerContext,
                            out topLevelDependencyMatches))
                        {
                            if (matchedDependencies.Count == 0)
                            {
                                continue;
                            }

                            topLevelDependencyMatches = GetMatchingResultsForDependency(
                                topLevelDependency,
                                targetedSnapshot,
                                matchedDependencies,
                                cachedDependencyToMatchingResultsMap);
                        }

                        cachedDependencyToMatchingResultsMap[topLevelDependency.Id] = topLevelDependencyMatches;
                    }

                    if (topLevelDependencyMatches.Count == 0)
                    {
                        continue;
                    }

                    GraphNode topLevelNode = _builder.AddTopLevelGraphNode(
                        graphContext,
                        snapshot.ProjectPath,
                        topLevelDependency.ToViewModel(targetedSnapshot));

                    foreach (IDependency matchedDependency in topLevelDependencyMatches)
                    {
                        GraphNode matchedDependencyNode = _builder.AddGraphNode(
                            graphContext,
                            snapshot.ProjectPath,
                            topLevelNode,
                            matchedDependency.ToViewModel(targetedSnapshot));

                        graphContext.Graph.Links.GetOrCreate(
                            topLevelNode,
                            matchedDependencyNode,
                            label: null,
                            GraphCommonSchema.Contains);
                    }

                    if (topLevelNode != null)
                    {
                        // 'node' is a GraphNode for top level dependency (which is part of solution explorer tree)
                        // Setting ProjectItem category (and correct GraphNodeId) ensures that search graph appears 
                        // under right solution explorer hierarchy item
                        topLevelNode.AddCategory(CodeNodeCategories.ProjectItem);
                    }
                }

                scope.Complete();
            }

            graphContext.OnCompleted();
        }

        /// <summary>
        /// Does flat search among dependency world lists to find any dependencies that match search criteria.
        /// </summary>
        private static HashSet<IDependency> SearchFlat(string searchTerm, DependenciesSnapshot dependenciesSnapshot)
        {
            var matchedDependencies = new HashSet<IDependency>(DependencyIdComparer.Instance);

            foreach ((ITargetFramework _, TargetedDependenciesSnapshot targetedSnapshot) in dependenciesSnapshot.DependenciesByTargetFramework)
            {
                foreach ((string _, IDependency dependency) in targetedSnapshot.DependenciesWorld)
                {
                    if (dependency.Visible && dependency.Caption.IndexOf(searchTerm, StringComparisons.UserEnteredSearchTermIgnoreCase) != -1)
                    {
                        matchedDependencies.Add(dependency);
                    }
                }
            }

            return matchedDependencies;
        }

        private static HashSet<IDependency> GetMatchingResultsForDependency(
            IDependency rootDependency,
            TargetedDependenciesSnapshot snapshot,
            HashSet<IDependency> flatMatchingDependencies,
            Dictionary<string, HashSet<IDependency>> cachedPositiveResults)
        {
            var matchingNodes = new HashSet<IDependency>(DependencyIdComparer.Instance);

            foreach (string childDependency in rootDependency.DependencyIDs)
            {
                if (!snapshot.DependenciesWorld.TryGetValue(childDependency, out IDependency childDependencyMetadata))
                {
                    continue;
                }

                if (flatMatchingDependencies.Contains(childDependencyMetadata))
                {
                    matchingNodes.Add(childDependencyMetadata);
                }

                if (cachedPositiveResults.TryGetValue(childDependency, out HashSet<IDependency> cachedResults))
                {
                    matchingNodes.AddRange(cachedResults);
                    continue;
                }

                HashSet<IDependency> children = GetMatchingResultsForDependency(
                    childDependencyMetadata,
                    snapshot,
                    flatMatchingDependencies,
                    cachedPositiveResults);

                cachedPositiveResults[childDependency] = children;

                if (children.Count > 0)
                {
                    matchingNodes.AddRange(children);
                }
            }

            return matchingNodes;
        }
    }
}
