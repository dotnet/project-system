// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        /// Destruction of the Dataflow subscription happens when the parent component is disposed or unloaded.
        /// </para>
        /// </remarks>
        private sealed class Subscription : ISubscription
        {
            private readonly IUpToDateCheckConfiguredInputDataSource _inputDataSource;

            /// <summary>
            /// The active configured project. For a multi-targeting project, there will be only one instance of this
            /// subscription, and this field will be the active configuration (usually the first).
            /// </summary>
            private readonly ConfiguredProject _configuredProject;

            /// <summary>
            /// Used to synchronise updates to <see cref="_link"/> and <see cref="_disposeTokenSource"/>.
            /// </summary>
            private readonly object _lock = new();

            private readonly IUpToDateCheckHost _host;

            private readonly IUpToDateCheckStatePersistence _persistence;

            /// <summary>
            /// Prevent overlapping requests.
            /// </summary>
            private readonly AsyncSemaphore _semaphore = new(1);

            private int _disposed;

            /// <summary>
            /// Lazily constructed Dataflow subscription. Set back to <see langword="null"/> in <see cref="Dispose"/>.
            /// </summary>
            private IDisposable? _link;

            private ImmutableArray<ProjectConfiguration> _lastCheckedConfigurations = ImmutableArray<ProjectConfiguration>.Empty;

            /// <summary>
            /// Cancelled when this instance is disposed.
            /// </summary>
            private readonly CancellationTokenSource _disposeTokenSource = new();

            public Subscription(IUpToDateCheckConfiguredInputDataSource inputDataSource, ConfiguredProject configuredProject, IUpToDateCheckHost host, IUpToDateCheckStatePersistence persistence)
            {
                Requires.NotNull(inputDataSource, nameof(inputDataSource));
                Requires.NotNull(configuredProject, nameof(configuredProject));

                _inputDataSource = inputDataSource;
                _configuredProject = configuredProject;
                _host = host;
                _persistence = persistence;
            }

            public async Task<bool> RunAsync(
                Func<UpToDateCheckConfiguredInput, IUpToDateCheckStatePersistence, CancellationToken, Task<(bool UpToDate, ImmutableArray<ProjectConfiguration> CheckedConfigurations)>> func,
                CancellationToken cancellationToken)
            {
                using var cts = CancellationTokenExtensions.CombineWith(cancellationToken, _disposeTokenSource.Token);

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

                (bool upToDate, _lastCheckedConfigurations) = await func(state.Value, _persistence, token);

                return upToDate;
            }

            public async Task UpdateLastSuccessfulBuildStartTimeUtcAsync(DateTime lastSuccessfulBuildStartTimeUtc, bool isRebuild)
            {
                IEnumerable<ProjectConfiguration> configurations;
                
                if (isRebuild)
                {
                    // During a rebuild the fast up-to-date check is not called, so we cannot rely upon
                    // _lastCheckedConfigurations having been updated. We currently only support building
                    // a subset of configurations for build, not for rebuild, so replace _lastCheckedConfigurations
                    // with the set of active configurations, as that is what was built.
                    IConfigurationGroup<ConfiguredProject> activeConfiguredProjects = await _configuredProject.UnconfiguredProject.Services.ActiveConfigurationGroupService.GetActiveLoadedConfiguredProjectGroupAsync();
                    configurations = activeConfiguredProjects.Select(p => p.ProjectConfiguration);
                }
                else
                {
                    configurations = _lastCheckedConfigurations;
                }

                await Task.WhenAll(configurations.Select(StoreConfigurationAsync));

                Task StoreConfigurationAsync(ProjectConfiguration configuration)
                {
                    return _persistence.StoreLastSuccessfulBuildStateAsync(
                        _configuredProject.UnconfiguredProject.FullPath,
                        configuration.Dimensions,
                        lastSuccessfulBuildStartTimeUtc,
                        CancellationToken.None);
                }
            }

            public void EnsureInitialized()
            {
                if (_link is not null || _disposed != 0)
                {
                    // Already initialized or disposed
                    return;
                }

                // Double check within lock
                lock (_lock)
                {
                    if (_link is null && _disposed == 0)
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

        /// <summary>
        /// Holds state in the active configuration.
        /// In a multi-targeting project (or one with any extra dimensions) this will be the first, in evaluation order.
        /// Per-target state is modelled in individual UpToDateCheckConfiguredInput values.
        /// </summary>
        internal interface ISubscription : IDisposable
        {
            /// <summary>
            /// Ensures the subscription has been initialized. Has no effect if the object has already been disposed.
            /// </summary>
            void EnsureInitialized();

            /// <summary>
            /// Notifies the subscription of the most recent time a successful build started.
            /// </summary>
            /// <remarks>
            /// Implementation must ensure only the most recently built configurations are updated.
            /// The most recently built configurations should be obtained from the return value of the
            /// function passed to <see cref="RunAsync"/>.
            /// </remarks>
            Task UpdateLastSuccessfulBuildStartTimeUtcAsync(DateTime lastSuccessfulBuildStartTimeUtc, bool isRebuild);

            /// <summary>
            /// Calls <paramref name="func"/> to determine whether the project is up-to-date or not.
            /// </summary>
            /// <param name="func">
            /// A function that accepts three arguments:
            /// <list type="number">
            ///     <item>The current project state as an instance of <see cref="UpToDateCheckConfiguredInput"/>.</item>
            ///     <item>An object that stores persisted data, such as the last successful build start time.</item>
            ///     <item>A cancellation token that may indicate a loss of interest in the result.</item>
            /// </list>
            /// And returns a tuple of:
            /// <list type="number">
            ///     <item>A boolean indicating whether the project is up-to-date or not.</item>
            ///     <item>The list of project configurations actually checked.</item>
            /// </list>
            /// </param>
            /// <param name="cancellationToken">Indicates a loss of interest in the result.</param>
            /// <returns><see langword="true"/> if the project is up-to-date, otherwise <see langword="false"/>.</returns>
            Task<bool> RunAsync(
                Func<UpToDateCheckConfiguredInput, IUpToDateCheckStatePersistence, CancellationToken, Task<(bool UpToDate, ImmutableArray<ProjectConfiguration> CheckedConfigurations)>> func,
                CancellationToken cancellationToken);
        }
    }
}
