// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    [Export(typeof(IDependenciesGraphChangeTracker))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class DependenciesGraphChangeTracker : IDependenciesGraphChangeTracker
    {
        private readonly object _snapshotChangeHandlerLock = new object();

        /// <summary>
        /// Remembers expanded graph nodes to track changes in their children.
        /// </summary>
        private readonly WeakCollection<IGraphContext> _expandedGraphContexts = new WeakCollection<IGraphContext>();

        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependenciesGraphActionHandler> _graphActionHandlers;

        private readonly IAggregateDependenciesSnapshotProvider _aggregateSnapshotProvider;

        [ImportingConstructor]
        public DependenciesGraphChangeTracker(IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
        {
            _aggregateSnapshotProvider = aggregateSnapshotProvider;
            _aggregateSnapshotProvider.SnapshotChanged += OnSnapshotChanged;

            _graphActionHandlers = new OrderPrecedenceImportCollection<IDependenciesGraphActionHandler>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast);
        }

        public void RegisterGraphContext(IGraphContext context)
        {
            bool shouldTrackChanges = false;

            foreach (Lazy<IDependenciesGraphActionHandler, IOrderPrecedenceMetadataView> handler in _graphActionHandlers)
            {
                if (handler.Value.CanHandleRequest(context) &&
                    handler.Value.HandleRequest(context))
                {
                    shouldTrackChanges = true;
                    break;
                }
            }

            if (!shouldTrackChanges)
            {
                return;
            }

            lock (_expandedGraphContexts)
            {
                if (!_expandedGraphContexts.Contains(context))
                {
                    // Remember this graph context in order to track changes.
                    // When references change, we will adjust children of this graph as necessary
                    _expandedGraphContexts.Add(context);
                }
            }
        }

        /// <summary>
        /// ProjectContextChanged gets fired every time dependencies change for projects across solution.
        /// <see cref="_expandedGraphContexts"/> contains all nodes that we need to check for potential updates
        /// in their children dependencies.
        /// </summary>
        private void OnSnapshotChanged(object sender, SnapshotChangedEventArgs e)
        {
            if (e.Snapshot == null || e.Token.IsCancellationRequested)
            {
                return;
            }

            // _expandedGraphContexts remembers graph expanded or checked so far.
            // Each context represents one level in the graph, i.e. a node and its first level dependencies
            // Tracking changes over all expanded contexts ensures that all levels are processed
            // and updated when there are any changes in nodes data.
            lock (_snapshotChangeHandlerLock)
            {
                IList<IGraphContext> expandedContexts;
                lock (_expandedGraphContexts)
                {
                    expandedContexts = _expandedGraphContexts.ToList();
                }

                if (expandedContexts.Count == 0)
                {
                    return;
                }

                var actionHandlers = _graphActionHandlers.Select(x => x.Value).Where(x => x.CanHandleChanges()).ToList();

                if (actionHandlers.Count == 0)
                {
                    return;
                }

                foreach (IGraphContext graphContext in expandedContexts)
                {
                    try
                    {
                        foreach (IDependenciesGraphActionHandler actionHandler in actionHandlers)
                        {
                            actionHandler.HandleChanges(graphContext, e);
                        }
                    }
                    finally
                    {
                        // Calling OnCompleted ensures that the changes are reflected in UI
                        graphContext.OnCompleted();
                    }
                }
            }
        }

        public void Dispose()
        {
            _aggregateSnapshotProvider.SnapshotChanged -= OnSnapshotChanged;
        }
    }
}
