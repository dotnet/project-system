// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
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
        /// This class contains all the state associated with such a lifetime: it's Dataflow subscription, tracking the first value to arrive,
        /// and the <see cref="BuildUpToDateCheck.State"/> instance.
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
            private readonly ConfiguredProject _configuredProject;
            private readonly IProjectItemSchemaService _projectItemSchemaService;

            /// <summary>
            /// Completes when the first project update is received. Cancelled if the subscription is disposed.
            /// </summary>
            /// <remarks>
            /// This field is also used to synchronise updates to <see cref="_link"/> and <see cref="State"/>.
            /// </remarks>
            private readonly TaskCompletionSource<byte> _dataReceived = new();

            /// <summary>
            /// Prevent overlapping requests.
            /// </summary>
            private readonly AsyncSemaphore _semaphore = new(1);

            /// <summary>
            /// Current <see cref="BuildUpToDateCheck.State"/> of the instance.
            /// </summary>
            /// <remarks>
            /// Internal to support unit testing only.
            /// </remarks>
            internal State State { get; set; } = State.Empty;

            /// <summary>
            /// Lazily constructed Dataflow subscription. Set back to <see langword="null"/> in <see cref="Dispose"/>.
            /// </summary>
            private IDisposable? _link;

            /// <summary>
            /// Cancelled when this instance is disposed.
            /// </summary>
            private readonly CancellationTokenSource _disposeTokenSource = new();

            public Subscription(ConfiguredProject configuredProject, IProjectItemSchemaService projectItemSchemaService)
            {
                Requires.NotNull(configuredProject, nameof(configuredProject));
                Requires.NotNull(projectItemSchemaService, nameof(projectItemSchemaService));

                _configuredProject = configuredProject;
                _projectItemSchemaService = projectItemSchemaService;
            }

            public async Task<bool> RunAsync(Func<State, CancellationToken, Task<bool>> func, CancellationToken cancellationToken)
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

                // Wait for the first state to be computed
                await _dataReceived.Task.WithCancellation(token);

                // Prevent overlapping requests
                using AsyncSemaphore.Releaser _ = await _semaphore.EnterAsync(token);

                bool result = await func(State, token);

                lock (_dataReceived)
                {
                    State = State.WithLastCheckedAtUtc(DateTime.UtcNow);
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

                lock (_dataReceived)
                {
                    // Double check within lock
                    if (_link == null)
                    {
                        Assumes.Present(_configuredProject.Services.ProjectSubscription);

                        _link = ProjectDataSources.SyncLinkTo(
                            _configuredProject.Services.ProjectSubscription.JointRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(ProjectPropertiesSchemas)),
                            _configuredProject.Services.ProjectSubscription.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                            _configuredProject.Services.ProjectSubscription.ProjectSource.SourceBlock.SyncLinkOptions(),
                            _projectItemSchemaService.SourceBlock.SyncLinkOptions(),
                            _configuredProject.Services.ProjectSubscription.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                            target: DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<ValueTuple<IProjectSubscriptionUpdate, IProjectSubscriptionUpdate, IProjectSnapshot, IProjectItemSchema, IProjectCatalogSnapshot>>>(OnChanged, _configuredProject.UnconfiguredProject),
                            linkOptions: DataflowOption.PropagateCompletion,
                            CancellationToken.None);
                    }
                }
            }

            internal void OnChanged(IProjectVersionedValue<ValueTuple<IProjectSubscriptionUpdate, IProjectSubscriptionUpdate, IProjectSnapshot, IProjectItemSchema, IProjectCatalogSnapshot>> e)
            {
                var snapshot = e.Value.Item3 as IProjectSnapshot2;
                Assumes.NotNull(snapshot);

                lock (_dataReceived)
                {
                    if (_disposeTokenSource.IsCancellationRequested)
                    {
                        // We've been disposed, so don't update State (which will be empty)
                        return;
                    }

                    State = State.Update(
                        jointRuleUpdate: e.Value.Item1,
                        sourceItemsUpdate: e.Value.Item2,
                        projectSnapshot: snapshot,
                        projectItemSchema: e.Value.Item4,
                        projectCatalogSnapshot: e.Value.Item5,
                        configuredProjectVersion: e.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion]);
                }

                _dataReceived.TrySetResult(0);
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

                lock (_dataReceived)
                {
                    _link?.Dispose();
                    _link = null;

                    State = State.Empty;

                    _disposeTokenSource.Cancel();
                    _disposeTokenSource.Dispose();
                }

                _dataReceived.TrySetCanceled();
            }
        }
    }
}
