// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceContextHost
    {
        /// <summary>
        ///     Responsible for lifetime of a <see cref="IWorkspaceProjectContext"/> and appling changes to a 
        ///     project to the context via the <see cref="IApplyChangesToWorkspaceContext"/> service.
        /// </summary>
        internal partial class WorkspaceContextHostInstance : OnceInitializedOnceDisposedUnderLockAsync, IMultiLifetimeInstance
        {
            private readonly ConfiguredProject _project;
            private readonly IProjectSubscriptionService _projectSubscriptionService;
            private readonly IProjectThreadingService _threadingService;
            private readonly IUnconfiguredProjectTasksService _tasksService;
            private readonly IWorkspaceProjectContextProvider _workspaceProjectContextProvider;
            private readonly IActiveWorkspaceProjectContextTracker _activeWorkspaceProjectContextTracker;
            private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;

            private DisposableBag _subscriptions;
            private IWorkspaceProjectContextAccessor _contextAccessor;
            private ExportLifetimeContext<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContext;

            public WorkspaceContextHostInstance(ConfiguredProject project,
                                                IProjectThreadingService threadingService,
                                                IUnconfiguredProjectTasksService tasksService,
                                                IProjectSubscriptionService projectSubscriptionService,
                                                IWorkspaceProjectContextProvider workspaceProjectContextProvider,
                                                IActiveWorkspaceProjectContextTracker activeWorkspaceProjectContextTracker,
                                                ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _projectSubscriptionService = projectSubscriptionService;
                _threadingService = threadingService;
                _tasksService = tasksService;
                _workspaceProjectContextProvider = workspaceProjectContextProvider;
                _applyChangesToWorkspaceContextFactory = applyChangesToWorkspaceContextFactory;
                _activeWorkspaceProjectContextTracker = activeWorkspaceProjectContextTracker;
            }

            public Task InitializeAsync()
            {
                return InitializeAsync(CancellationToken.None);
            }

            protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                _contextAccessor = await _workspaceProjectContextProvider.CreateProjectContextAsync(_project)
                                                                         .ConfigureAwait(true);

                if (_contextAccessor == null)
                    return;

                _activeWorkspaceProjectContextTracker.RegisterContext(_contextAccessor.Context, _contextAccessor.ContextId);

                _applyChangesToWorkspaceContext = _applyChangesToWorkspaceContextFactory.CreateExport();
                _applyChangesToWorkspaceContext.Value.Initialize(_contextAccessor.Context);

                _subscriptions = new DisposableBag(CancellationToken.None);
                _subscriptions.AddDisposable(_projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkToAsyncAction(
                        target: e => OnProjectChangedAsync(e, evaluation: true),
                        ruleNames: _applyChangesToWorkspaceContext.Value.GetProjectEvaluationRules()));

                _subscriptions.AddDisposable(_projectSubscriptionService.ProjectBuildRuleSource.SourceBlock.LinkToAsyncAction(
                        target: e => OnProjectChangedAsync(e, evaluation: false),
                        ruleNames: _applyChangesToWorkspaceContext.Value.GetProjectBuildRules()));
            }

            protected override async Task DisposeCoreUnderLockAsync(bool initialized)
            {
                if (initialized)
                {
                    _subscriptions?.Dispose();
                    _applyChangesToWorkspaceContext?.Dispose();

                    if (_contextAccessor != null)
                    {
                        _activeWorkspaceProjectContextTracker.UnregisterContext(_contextAccessor.Context);

                        await _workspaceProjectContextProvider.ReleaseProjectContextAsync(_contextAccessor)
                                                              .ConfigureAwait(true);
                    }
                }
            }

            public async Task OpenContextForWriteAsync(Func<IWorkspaceProjectContextAccessor, Task> action)
            {
                await WaitUntilInitializedCompletedAsync();

                // TODO: https://github.com/dotnet/project-system/issues/353
                await _threadingService.SwitchToUIThread(_tasksService.UnloadCancellationToken);

                await ExecuteUnderLockAsync(_ => action(_contextAccessor), _tasksService.UnloadCancellationToken);
            }

            public async Task<T> OpenContextForWriteAsync<T>(Func<IWorkspaceProjectContextAccessor, Task<T>> action)
            {
                await WaitUntilInitializedCompletedAsync();

                // TODO: https://github.com/dotnet/project-system/issues/353
                await _threadingService.SwitchToUIThread(_tasksService.UnloadCancellationToken);

                return await ExecuteUnderLockAsync(_ => action(_contextAccessor), _tasksService.UnloadCancellationToken);
            }

            internal async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool evaluation)
            {
                CancellationToken cancellationToken = _tasksService.UnloadCancellationToken;

                // TODO: https://github.com/dotnet/project-system/issues/353
                await _threadingService.SwitchToUIThread(cancellationToken);

                await ExecuteUnderLockAsync(ct =>
                {
                    return ApplyProjectChangesUnderLockAsync(update, evaluation, ct);

                }, cancellationToken);
            }

            private Task ApplyProjectChangesUnderLockAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool evaluation, CancellationToken cancellationToken)
            {
                bool isActiveContext = _activeWorkspaceProjectContextTracker.IsActiveContext(_contextAccessor.Context);

                if (evaluation)
                {
                    return _applyChangesToWorkspaceContext.Value.ApplyProjectEvaluationAsync(update, isActiveContext, cancellationToken);
                }
                else
                {
                    return _applyChangesToWorkspaceContext.Value.ApplyProjectBuildAsync(update, isActiveContext, cancellationToken);
                }
            }

            private async Task WaitUntilInitializedCompletedAsync()
            {
                await InitializationCompletion;

                // If we failed to create a context, we treat it as a cancellation
                if (_contextAccessor == null)
                    throw new OperationCanceledException();
            }
        }
    }
}
