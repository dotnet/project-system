// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        /// <summary>
        /// Contains and tracks state related to a lifetime instance of <see cref="BuildUpToDateCheck"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// As the parent <see cref="BuildUpToDateCheck"/> is an <see cref="IProjectDynamicLoadComponent"/>, it may have multiple lifetimes.
        /// This class contains all the state associated with such a lifetime: its Dataflow subscription, tracking the first value to arrive,
        /// and the <see cref="UpToDateCheckConfiguredInput"/> instance.
        /// </para>
        /// <para>
        /// Initialization of the Dataflow subscription happens lazily, upon the first up-to-date check request.
        /// </para>
        /// <para>
        /// Destruction of the Dataflow subscription happens when the parent component is disposed or unloaded.
        /// </para>
        /// </remarks>
        private sealed class Subscription : IDisposable
        {
            private readonly IUpToDateCheckConfiguredInputDataSource _inputDataSource;

            private readonly ConfiguredProject _configuredProject;

            /// <summary>
            /// Used to synchronise updates to <see cref="_link"/> and <see cref="State"/>.
            /// </summary>
            private readonly object _lock = new();

            /// <summary>
            /// Prevent overlapping requests.
            /// </summary>
            private readonly AsyncSemaphore _semaphore = new(1);

            /// <summary>
            /// Internal for testing purposes only.
            /// </summary>
            internal UpToDateCheckConfiguredInput? State { get; private set; }

            /// <summary>
            /// Gets the time at which the last up-to-date check was made.
            /// </summary>
            /// <remarks>
            /// This value is required in order to protect against a race condition described in
            /// https://github.com/dotnet/project-system/issues/4014. Specifically, if source files are
            /// modified during a compilation, but before that compilation's outputs are produced, then
            /// the changed input file's timestamp will be earlier than the compilation output, making
            /// it seem as though the compilation is up to date when in fact the input was not included
            /// in that compilation. We use this property as a proxy for compilation start time, whereas
            /// the outputs represent compilation end time.
            /// </remarks>
            internal DateTime LastCheckedAtUtc { get; set; } = DateTime.MinValue;

            /// <summary>
            /// Lazily constructed Dataflow subscription. Set back to <see langword="null"/> in <see cref="Dispose"/>.
            /// </summary>
            private IDisposable? _link;

            /// <summary>
            /// Cancelled when this instance is disposed.
            /// </summary>
            private readonly CancellationTokenSource _disposeTokenSource = new();

            public Subscription(IUpToDateCheckConfiguredInputDataSource inputDataSource, ConfiguredProject configuredProject)
            {
                Requires.NotNull(inputDataSource, nameof(inputDataSource));
                Requires.NotNull(configuredProject, nameof(configuredProject));

                _inputDataSource = inputDataSource;
                _configuredProject = configuredProject;
            }

            public async Task<bool> RunAsync(Func<UpToDateCheckConfiguredInput, DateTime, CancellationToken, Task<bool>> func, CancellationToken cancellationToken)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeTokenSource.Token);

                CancellationToken token = cts.Token;

                // Throws if the subscription has been disposed, or the caller's token cancelled.
                token.ThrowIfCancellationRequested();

                // Note that we defer subscription until an actual request is made in order to
                // prevent redundant work/allocation for inactive project configurations.
                // https://github.com/dotnet/project-system/issues/6327
                //
                // We don't pass the cancellation token here as initialization must be atomic.
                EnsureInitialized();

                token.ThrowIfCancellationRequested();

                // TODO wait for our state to be up to date with that of the project (https://github.com/dotnet/project-system/issues/6185)

                if (State == null)
                {
                    // We have either haven't received data yet, or have been disposed.
                    return false;
                }

                // Prevent overlapping requests
                using AsyncSemaphore.Releaser _ = await _semaphore.EnterAsync(token);

                bool result = await func(State, LastCheckedAtUtc, token);

                lock (_lock)
                {
                    LastCheckedAtUtc = DateTime.UtcNow;
                }

                return result;
            }

            public void EnsureInitialized()
            {
                if (_link != null)
                {
                    // Already initialized (or disposed)
                    return;
                }

                lock (_lock)
                {
                    // Double check within lock
                    if (_link == null)
                    {
                        ITargetBlock<IProjectVersionedValue<UpToDateCheckConfiguredInput>> actionBlock
                            = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<UpToDateCheckConfiguredInput>>(OnChanged, _configuredProject.UnconfiguredProject);

                        _link = _inputDataSource.SourceBlock.LinkTo(actionBlock, DataflowOption.PropagateCompletion);
                    }
                }
            }

            internal void OnChanged(IProjectVersionedValue<UpToDateCheckConfiguredInput> e)
            {
                lock (_lock)
                {
                    if (_disposeTokenSource.IsCancellationRequested)
                    {
                        // We've been disposed, so don't update State (which will be null)
                        return;
                    }

                    State = e.Value;
                }
            }

            /// <summary>
            /// Tear down any Dataflow subscription and cancel any ongoing query.
            /// </summary>
            public void Dispose()
            {
                if (_disposeTokenSource.IsCancellationRequested)
                {
                    // Already disposed
                    return;
                }

                lock (_lock)
                {
                    _link?.Dispose();
                    _link = null;

                    State = null;

                    _disposeTokenSource.Cancel();
                    _disposeTokenSource.Dispose();
                }
            }
        }
    }
}
