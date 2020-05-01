// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions
{
    internal sealed partial class DependenciesSnapshotProvider
    {
        /// <summary>
        /// Ensures the correct sequencing of updates to the dependencies snapshot, and appropriate debouncing
        /// and propagation of updates to downstream consumers of the snapshot.
        /// </summary>
        private sealed class SnapshotUpdater : IDisposable
        {
            private readonly object _lock = new object();

            private readonly IBroadcastBlock<SnapshotChangedEventArgs> _source;
            private readonly ITaskDelayScheduler _debounce;

            private DependenciesSnapshot _currentSnapshot;
            private int _isDisposed;

            public SnapshotUpdater(IProjectThreadingService projectThreadingService, CancellationToken unloadCancellationToken)
            {
                // Initial snapshot is empty.
                _currentSnapshot = DependenciesSnapshot.CreateEmpty();

                // Updates will be published via Dataflow.
                _source = DataflowBlockSlim.CreateBroadcastBlock<SnapshotChangedEventArgs>("DependenciesSnapshot {1}", skipIntermediateInputData: true);

                // Updates are debounced to conflate rapid updates and reduce frequency of tree updates downstream.
                _debounce = new TaskDelayScheduler(
                    TimeSpan.FromMilliseconds(250),
                    projectThreadingService,
                    unloadCancellationToken);
            }

            public DependenciesSnapshot Current => _currentSnapshot;

            public IReceivableSourceBlock<SnapshotChangedEventArgs> Source => _source;

            /// <summary>
            /// Executes <paramref name="updateFunc"/> on the current snapshot within a lock. If a new snapshot
            /// object is returned, <see cref="Current"/> is updated and the update is posted to <see cref="Source"/>.
            /// </summary>
            public void TryUpdate(Func<DependenciesSnapshot, DependenciesSnapshot> updateFunc, CancellationToken token = default)
            {
                if (_isDisposed != 0)
                {
                    throw new ObjectDisposedException(nameof(SnapshotUpdater));
                }

                lock (_lock)
                {
                    DependenciesSnapshot updatedSnapshot = updateFunc(_currentSnapshot);

                    if (ReferenceEquals(_currentSnapshot, updatedSnapshot))
                    {
                        return;
                    }

                    _currentSnapshot = updatedSnapshot;
                }

                // Conflate rapid snapshot updates by debouncing events over a short window.
                // This reduces the frequency of tree updates with minimal perceived latency.
                _debounce.ScheduleAsyncTask(
                    ct =>
                    {
                        if (ct.IsCancellationRequested || _isDisposed != 0)
                        {
                            return Task.FromCanceled(ct);
                        }

                        // Always publish the latest snapshot
                        DependenciesSnapshot snapshot = _currentSnapshot;
                        _source.Post(new SnapshotChangedEventArgs(snapshot, ct));

                        return Task.CompletedTask;
                    }, token);
            }

            public void Dispose()
            {
                // Ensure we don't double-dispose.
                if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
                {
                    _debounce.Dispose();
                    _source.Complete();
                }
            }
        }
    }
}
