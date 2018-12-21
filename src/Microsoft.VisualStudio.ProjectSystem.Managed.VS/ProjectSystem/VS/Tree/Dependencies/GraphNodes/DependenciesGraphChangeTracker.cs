// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    /// <summary>
    /// Listens to aggregate snapshot changes and updates known graph contexts accordingly.
    /// </summary>
    [Export(typeof(IDependenciesGraphChangeTracker))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class DependenciesGraphChangeTracker : IDependenciesGraphChangeTracker
    {
        private readonly object _lock = new object();

        /// <summary>
        /// Remembers expanded graph nodes to track changes in their children.
        /// </summary>
        private readonly WeakCollection<IGraphContext> _expandedGraphContexts = new WeakCollection<IGraphContext>();

        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependenciesGraphViewProvider> _viewProviders;

        private readonly IAggregateDependenciesSnapshotProvider _aggregateSnapshotProvider;

        [ImportingConstructor]
        public DependenciesGraphChangeTracker(IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
        {
            _aggregateSnapshotProvider = aggregateSnapshotProvider;
            _aggregateSnapshotProvider.SnapshotChanged += OnSnapshotChanged;

            _viewProviders = new OrderPrecedenceImportCollection<IDependenciesGraphViewProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst);
        }

        public void RegisterGraphContext(IGraphContext context)
        {
            lock (_lock)
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
            lock (_lock)
            {
                foreach (IGraphContext graphContext in _expandedGraphContexts)
                {
                    bool anyChanges = false;

                    try
                    {
                        if (HandleChanges(graphContext, e))
                        {
                            anyChanges = true;
                        }
                    }
                    finally
                    {
                        // Calling OnCompleted ensures that the changes are reflected in UI
                        if (anyChanges)
                        {
                            graphContext.OnCompleted();
                        }
                    }
                }
            }
        }

        private bool HandleChanges(IGraphContext graphContext, SnapshotChangedEventArgs e)
        {
            IDependenciesSnapshot snapshot = e.Snapshot;

            if (snapshot == null || e.Token.IsCancellationRequested)
            {
                return false;
            }

            bool anyChanges = false;

            foreach (GraphNode inputGraphNode in graphContext.InputNodes.ToList())
            {
                string existingDependencyId = inputGraphNode.GetValue<string>(DependenciesGraphSchema.DependencyIdProperty);
                if (string.IsNullOrEmpty(existingDependencyId))
                {
                    continue;
                }

                string nodeProjectPath = inputGraphNode.Id.GetValue(CodeGraphNodeIdName.Assembly);
                if (string.IsNullOrEmpty(nodeProjectPath))
                {
                    continue;
                }

                IDependenciesSnapshot updatedSnapshot = _aggregateSnapshotProvider.GetSnapshot(nodeProjectPath);

                IDependency updatedDependency = updatedSnapshot?.FindDependency(existingDependencyId);

                if (updatedDependency == null)
                {
                    continue;
                }

                IDependenciesGraphViewProvider viewProvider = _viewProviders
                    .FirstOrDefaultValue((x, d) => x.SupportsDependency(d), updatedDependency);
                if (viewProvider == null)
                {
                    continue;
                }

                if (!viewProvider.ShouldApplyChanges(nodeProjectPath, snapshot.ProjectPath, updatedDependency))
                {
                    continue;
                }

                using (var scope = new GraphTransactionScope())
                {
                    if (viewProvider.ApplyChanges(
                        graphContext,
                        nodeProjectPath,
                        updatedDependency,
                        inputGraphNode,
                        updatedSnapshot.Targets[updatedDependency.TargetFramework]))
                    {
                        anyChanges = true;
                    }

                    scope.Complete();
                }
            }

            return anyChanges;
        }

        public void Dispose()
        {
            _aggregateSnapshotProvider.SnapshotChanged -= OnSnapshotChanged;
        }

        private sealed class WeakCollection<T> where T : class
        {
            private readonly LinkedList<WeakReference> _references = new LinkedList<WeakReference>();

            public void Add(T item)
            {
                _references.AddLast(new WeakReference(item));
            }

            public bool Contains(T item)
            {
                foreach (T member in this)
                {
                    if (Equals(member, item))
                    {
                        return true;
                    }
                }

                return false;
            }

            public Enumerator GetEnumerator() => new Enumerator(_references);

            public struct Enumerator
            {
                private readonly LinkedList<WeakReference> _list;
                private LinkedListNode<WeakReference> _next;

                internal Enumerator(LinkedList<WeakReference> list)
                {
                    _list = list;
                    _next = list.First;
                    Current = null;
                }

                public T Current { get; private set; }

                public bool MoveNext()
                {
                    while (_next != null)
                    {
                        if (_next.Value.Target is T target)
                        {
                            // Reference is alive: yield it
                            Current = target;
                            _next = _next.Next;
                            return true;
                        }
                        else
                        {
                            // Reference has been collected: remove it and continue
                            LinkedListNode<WeakReference> remove = _next;
                            _next = _next.Next;
                            _list.Remove(remove);
                        }
                    }

                    Current = null;
                    return false;
                }
            }
        }
    }
}
