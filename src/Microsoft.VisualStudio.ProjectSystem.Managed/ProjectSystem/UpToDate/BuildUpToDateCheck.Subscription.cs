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
        private sealed class Subscription : ISubscription
        {
            private readonly IUpToDateCheckConfiguredInputDataSource _inputDataSource;

            private readonly ConfiguredProject _configuredProject;

            /// <summary>
            /// Used to synchronise updates to <see cref="_link"/> and <see cref="_disposeTokenSource"/>.
            /// </summary>
            private readonly object _lock = new();

            private readonly IUpToDateCheckHost _host;

            /// <summary>
            /// Prevent overlapping requests.
            /// </summary>
            private readonly AsyncSemaphore _semaphore = new(1);

            private int _disposed;

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
            private DateTime _lastCheckedAtUtc = DateTime.MinValue;

            /// <summary>
            /// Lazily constructed Dataflow subscription. Set back to <see langword="null"/> in <see cref="Dispose"/>.
            /// </summary>
            private IDisposable? _link;

            /// <summary>
            /// Cancelled when this instance is disposed.
            /// </summary>
            private readonly CancellationTokenSource _disposeTokenSource = new();

            public Subscription(IUpToDateCheckConfiguredInputDataSource inputDataSource, ConfiguredProject configuredProject, IUpToDateCheckHost host)
            {
                Requires.NotNull(inputDataSource, nameof(inputDataSource));
                Requires.NotNull(configuredProject, nameof(configuredProject));

                _inputDataSource = inputDataSource;
                _configuredProject = configuredProject;
                _host = host;
            }

            public async Task<bool> RunAsync(Func<UpToDateCheckConfiguredInput, DateTime, CancellationToken, Task<bool>> func, CancellationToken cancellationToken)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeTokenSource.Token);

                CancellationToken token = cts.Token;

                // Throws if the subscription has been disposed, or the caller's token cancelled.
                token.ThrowIfCancellationRequested();

                if (!await _host.HasDesignTimeBuildsAsync(token))
                {
                    // Design time builds aren't available in the host. This can happen when running in command line mode, for example.
                    // In such a case we will not have the data we need. Presume the project is not up-to-date.
                    return false;
                }

                EnsureInitialized();

                if (_disposed != 0)
                {
                    // We have been disposed
                    return false;
                }

                Assumes.NotNull(_link);

                // Prevent overlapping requests
                using AsyncSemaphore.Releaser _ = await _semaphore.EnterAsync(token);

                token.ThrowIfCancellationRequested();

                IProjectVersionedValue<UpToDateCheckConfiguredInput> state;

                using (_inputDataSource.Join())
                {
                    // Wait for our state to be up to date with that of the project
                    state = await _inputDataSource.SourceBlock.GetLatestVersionAsync(
                        _configuredProject,
                        cancellationToken: token);
                }

                bool result = await func(state.Value, _lastCheckedAtUtc, token);

                _lastCheckedAtUtc = DateTime.UtcNow;

                return result;
            }

            public void EnsureInitialized()
            {
                if (_link != null || _disposed != 0)
                {
                    // Already initialized or disposed
                    return;
                }

                // Double check within lock
                lock (_lock)
                {
                    if (_link == null && _disposed == 0)
                    {
                        // Link to a null target so that data will flow. The null target drops all values.
                        // When we need a value we use GetLatestVersionAsync to ensure the UpToDateCheckConfiguredInput
                        // is in sync with the current configured project.

                        ITargetBlock<IProjectVersionedValue<UpToDateCheckConfiguredInput>> target
                            = DataflowBlock.NullTarget<IProjectVersionedValue<UpToDateCheckConfiguredInput>>();

                        _link = _inputDataSource.SourceBlock.LinkTo(target, DataflowOption.PropagateCompletion);
                    }
                }
            }

            /// <summary>
            /// Cancel any ongoing query and release Dataflow subscription.
            /// </summary>
            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                {
                    // Already disposed
                    return;
                }

                lock (_lock)
                {
                    _link?.Dispose();
                    _link = null;

                    _disposeTokenSource.Cancel();
                    _disposeTokenSource.Dispose();
                }
            }
        }

        internal interface ISubscription : IDisposable
        {
            void EnsureInitialized();

            Task<bool> RunAsync(
                Func<UpToDateCheckConfiguredInput, DateTime, CancellationToken, Task<bool>> func,
                CancellationToken cancellationToken);
        }
    }
}
