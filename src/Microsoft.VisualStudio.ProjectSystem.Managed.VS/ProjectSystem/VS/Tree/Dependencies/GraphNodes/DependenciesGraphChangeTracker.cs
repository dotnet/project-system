// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    [Export(typeof(IDependenciesGraphChangeTracker))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class DependenciesGraphChangeTracker : IDependenciesGraphChangeTracker
    {
        private readonly object _lock = new object();

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
            foreach (Lazy<IDependenciesGraphActionHandler, IOrderPrecedenceMetadataView> handler in _graphActionHandlers)
            {
                if (handler.Value.CanHandleRequest(context) &&
                    handler.Value.HandleRequest(context))
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

                    // Only one handler should succeed
                    return;
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
                var actionHandlers = _graphActionHandlers.Select(x => x.Value).Where(x => x.CanHandleChanges()).ToList();

                if (actionHandlers.Count == 0)
                {
                    return;
                }

                foreach (IGraphContext graphContext in _expandedGraphContexts)
                {
                    bool anyChanges = false;

                    try
                    {
                        foreach (IDependenciesGraphActionHandler actionHandler in actionHandlers)
                        {
                            if (actionHandler.HandleChanges(graphContext, e))
                            {
                                anyChanges = true;
                            }
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
