// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    [Export(typeof(IDependenciesGraphActionHandler))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class SearchGraphActionHandler : GraphActionHandlerBase
    {
        public const int Order = 120;

        [ImportingConstructor]
        public SearchGraphActionHandler(IDependenciesGraphBuilder builder,
                                        IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(builder, aggregateSnapshotProvider)
        {
        }

        public override bool CanHandleRequest(IGraphContext graphContext)
        {
            return graphContext.Direction == GraphContextDirection.Custom;
        }

        public override bool HandleRequest(IGraphContext graphContext)
        {
            Search(graphContext);

            return false;
        }

        /// <summary>
        /// Generates search graph containing nodes matching search criteria in Solution Explorer 
        /// and attaches it to correct top level node.
        /// </summary>
        private void Search(IGraphContext graphContext)
        {
            var searchParametersTypeName = typeof(ISolutionSearchParameters).GUID.ToString();
            var searchParameters = graphContext.GetValue<ISolutionSearchParameters>(searchParametersTypeName);
            if (searchParameters == null)
            {
                return;
            }

            var searchTerm = searchParameters.SearchQuery.SearchString?.ToLowerInvariant();
            if (searchTerm == null)
            {
                return;
            }

            var cachedDependencyToMatchingResultsMap = new Dictionary<string, HashSet<IDependency>>(StringComparer.OrdinalIgnoreCase);
            var searchResultsPerContext = new Dictionary<string, HashSet<IDependency>>(StringComparer.OrdinalIgnoreCase);

            var snapshotProviders = AggregateSnapshotProvider.GetSnapshotProviders();
            foreach (var snapshotProvider in snapshotProviders)
            {
                var snapshot = snapshotProvider.CurrentSnapshot;
                if (snapshot == null)
                {
                    continue;
                }

                searchResultsPerContext[snapshotProvider.ProjectFilePath] = SearchFlat(snapshotProvider.ProjectFilePath,
                                                                                     searchTerm.ToLowerInvariant(),
                                                                                     graphContext,
                                                                                     snapshot);
            }

            foreach (var snapshotProvider in snapshotProviders)
            {
                var snapshot = snapshotProvider.CurrentSnapshot;
                if (snapshot == null)
                {
                    continue;
                }

                var allTopLevelDependencies = snapshot.GetFlatTopLevelDependencies();
                var matchedDependencies = searchResultsPerContext[snapshotProvider.ProjectFilePath];

                using (var scope = new GraphTransactionScope())
                {
                    foreach (var topLevelDependency in allTopLevelDependencies)
                    {
                        var targetedSnapshot = snapshot.Targets[topLevelDependency.TargetFramework];

                        if (!cachedDependencyToMatchingResultsMap
                                .TryGetValue(topLevelDependency.Id, out HashSet<IDependency> topLevelDependencyMatches))
                        {
                            var viewProvider = ViewProviders.FirstOrDefault(x => x.Value.SupportsDependency(topLevelDependency));
                            if (viewProvider == null)
                            {
                                continue;
                            }

                            var processed = viewProvider.Value.MatchSearchResults(
                                snapshotProvider.ProjectFilePath,
                                topLevelDependency,
                                searchResultsPerContext,
                                out topLevelDependencyMatches);

                            if (!processed)
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

                        var topLevelNode = Builder.AddTopLevelGraphNode(graphContext,
                                                                snapshotProvider.ProjectFilePath,
                                                                topLevelDependency.ToViewModel(targetedSnapshot));
                        foreach (var matchedDependency in topLevelDependencyMatches)
                        {
                            var matchedDependencyNode = Builder.AddGraphNode(graphContext,
                                                                    snapshotProvider.ProjectFilePath,
                                                                    topLevelNode,
                                                                    matchedDependency.ToViewModel(targetedSnapshot));

                            graphContext.Graph.Links.GetOrCreate(topLevelNode,
                                                                 matchedDependencyNode,
                                                                 null /*label*/,
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
            }

            graphContext.OnCompleted();
        }

        /// <summary>
        /// Does flat search among dependency world lists to find any dependencies that match search criteria.
        /// </summary>
        private HashSet<IDependency> SearchFlat(
            string projectPath,
            string searchTerm,
            IGraphContext graphContext,
            IDependenciesSnapshot dependenciesSnapshot)
        {
            var matchedDependencies = new HashSet<IDependency>();
            foreach (var targetedSnapshot in dependenciesSnapshot.Targets)
            {
                foreach (var dependency in targetedSnapshot.Value.DependenciesWorld)
                {
                    if (dependency.Value.Visible && dependency.Value.Caption.ToLowerInvariant().Contains(searchTerm))
                    {
                        matchedDependencies.Add(dependency.Value);
                    }
                }
            }

            return matchedDependencies;
        }

        private HashSet<IDependency> GetMatchingResultsForDependency(
            IDependency rootDependency,
            ITargetedDependenciesSnapshot snapshot,
            HashSet<IDependency> flatMatchingDependencies,
            Dictionary<string, HashSet<IDependency>> cachedPositiveResults)
        {
            var matchingNodes = new HashSet<IDependency>();
            foreach (var childDependency in rootDependency.DependencyIDs)
            {
                if (!snapshot.DependenciesWorld
                        .TryGetValue(childDependency, out IDependency childDependencyMetadata))
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

                var children = GetMatchingResultsForDependency(
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
