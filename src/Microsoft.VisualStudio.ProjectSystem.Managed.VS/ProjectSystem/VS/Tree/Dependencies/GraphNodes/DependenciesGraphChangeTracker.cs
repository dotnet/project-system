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

#nullable enable

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
        /// Remembers expanded graph nodes to track changes in their children. We don't control the lifetime
        /// of these objects, so use weak references to track which contexts are still alive. Elements in this
        /// collection will be forgotten once garbage collected.
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
        /// in their child dependencies.
        /// </summary>
        private void OnSnapshotChanged(object sender, SnapshotChangedEventArgs e)
        {
            IDependenciesSnapshot snapshot = e.Snapshot;

            if (snapshot == null || e.Token.IsCancellationRequested)
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
                    if (e.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    bool anyChanges = false;

                    try
                    {
                        if (HandleChanges(graphContext))
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

            return;

            bool HandleChanges(IGraphContext graphContext)
            {
                bool anyChanges = false;

                foreach (GraphNode inputGraphNode in graphContext.InputNodes.ToList())
                {
                    string? existingDependencyId = inputGraphNode.GetValue<string>(DependenciesGraphSchema.DependencyIdProperty);
                    if (string.IsNullOrEmpty(existingDependencyId))
                    {
                        continue;
                    }

                    string? nodeProjectPath = inputGraphNode.Id.GetValue(CodeGraphNodeIdName.Assembly);
                    if (string.IsNullOrEmpty(nodeProjectPath))
                    {
                        continue;
                    }

                    IDependenciesSnapshot? updatedSnapshot = _aggregateSnapshotProvider.GetSnapshot(nodeProjectPath);

                    IDependency? updatedDependency = updatedSnapshot?.FindDependency(existingDependencyId);

                    if (updatedDependency == null) // or updatedSnapshot == null
                    {
                        continue;
                    }

                    IDependenciesGraphViewProvider? viewProvider = _viewProviders
                        .FirstOrDefaultValue((x, d) => x.SupportsDependency(d), updatedDependency);
                    if (viewProvider == null)
                    {
                        continue;
                    }

                    if (!viewProvider.ShouldApplyChanges(nodeProjectPath, snapshot.ProjectPath, updatedDependency))
                    {
                        continue;
                    }

                    using var scope = new GraphTransactionScope();
                    if (viewProvider.ApplyChanges(
                        graphContext,
                        nodeProjectPath,
                        updatedDependency,
                        inputGraphNode,
                        updatedSnapshot!.Targets[updatedDependency.TargetFramework]))
                    {
                        anyChanges = true;
                    }

                    scope.Complete();
                }

                return anyChanges;
            }
        }

        public void Dispose()
        {
            _aggregateSnapshotProvider.SnapshotChanged -= OnSnapshotChanged;
        }

        /// <summary>
        /// Maintains a collection of objects via <see cref="WeakReference{T}"/>, allowing elements to be
        /// garbage collected.
        /// </summary>
        /// <remarks>
        /// Note this implementation exists intentionally, despite <see cref="PlatformUI.WeakCollection{T}"/>.
        /// It allocates less memory during use, and combines pruning with enumeration. Neither type is
        /// free-threaded.
        /// </remarks>
        /// <typeparam name="T">Type of objects tracked within this collection.</typeparam>
        private sealed class WeakCollection<T> where T : class
        {
            private readonly LinkedList<WeakReference<T>> _references = new LinkedList<WeakReference<T>>();

            public void Add(T item)
            {
                _references.AddLast(new WeakReference<T>(item));
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

            /// <summary>
            /// A struct enumerator for items within the weak collection, allowing enumeration without
            /// allocation. Dead references are cleaned up during enumeration.
            /// </summary>
            public struct Enumerator
            {
                private readonly LinkedList<WeakReference<T>> _list;
                private LinkedListNode<WeakReference<T>> _next;

                internal Enumerator(LinkedList<WeakReference<T>> list)
                {
                    _list = list;
                    _next = list.First;
                    Current = null!;
                }

                public T Current { get; private set; }

                public bool MoveNext()
                {
                    while (_next != null)
                    {
                        if (_next.Value.TryGetTarget(out T target))
                        {
                            // Reference is alive: yield it
                            Current = target;
                            _next = _next.Next;
                            return true;
                        }
                        else
                        {
                            // Reference has been collected: remove it and continue
                            LinkedListNode<WeakReference<T>> remove = _next;
                            _next = _next.Next;
                            _list.Remove(remove);
                        }
                    }

                    Current = null!;
                    return false;
                }
            }
        }
    }
}
