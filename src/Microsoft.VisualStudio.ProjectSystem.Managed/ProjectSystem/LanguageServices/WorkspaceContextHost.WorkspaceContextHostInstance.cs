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
            private readonly IUnconfiguredProjectTasksService _tasksService;
            private readonly IWorkspaceProjectContextProvider _workspaceProjectContextProvider;
            private readonly IActiveEditorContextTracker _activeWorkspaceProjectContextTracker;
            private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;

            private DisposableBag _subscriptions;
            private IWorkspaceProjectContextAccessor _contextAccessor;
            private ExportLifetimeContext<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContext;

            public WorkspaceContextHostInstance(ConfiguredProject project,
                                                IProjectThreadingService threadingService,
                                                IUnconfiguredProjectTasksService tasksService,
                                                IProjectSubscriptionService projectSubscriptionService,
                                                IWorkspaceProjectContextProvider workspaceProjectContextProvider,
                                                IActiveEditorContextTracker activeWorkspaceProjectContextTracker,
                                                ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _projectSubscriptionService = projectSubscriptionService;
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
                _contextAccessor = await _workspaceProjectContextProvider.CreateProjectContextAsync(_project);

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

                        await _workspaceProjectContextProvider.ReleaseProjectContextAsync(_contextAccessor);
                    }
                }
            }

            public async Task OpenContextForWriteAsync(Func<IWorkspaceProjectContextAccessor, Task> action)
            {
                CheckForInitialized();

                try
                {
                    await ExecuteUnderLockAsync(_ => action(_contextAccessor), _tasksService.UnloadCancellationToken);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == DisposalToken)
                {   // We treat cancellation because our instance was disposed differently from when the project is unloading.
                    // 
                    // The former indicates that the active configuration changed, and our ConfiguredProject is no longer 
                    // considered implicitly "active", we throw a different exceptions to let callers handle that.
                    throw new ActiveProjectConfigurationChangedException();
                }
            }

            public async Task<T> OpenContextForWriteAsync<T>(Func<IWorkspaceProjectContextAccessor, Task<T>> action)
            {
                CheckForInitialized();

                try
                {
                    return await ExecuteUnderLockAsync(_ => action(_contextAccessor), _tasksService.UnloadCancellationToken);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == DisposalToken)
                {
                    throw new ActiveProjectConfigurationChangedException();
                }
            }

            internal Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool evaluation)
            {
                return ExecuteUnderLockAsync(ct => ApplyProjectChangesUnderLockAsync(update, evaluation, ct), _tasksService.UnloadCancellationToken);
            }

            private async Task ApplyProjectChangesUnderLockAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool evaluation, CancellationToken cancellationToken)
            {
                IWorkspaceProjectContext context = _contextAccessor.Context;

                context.StartBatch();

                try
                {
                    bool isActiveContext = _activeWorkspaceProjectContextTracker.IsActiveEditorContext(context);

                    if (evaluation)
                    {
                        await _applyChangesToWorkspaceContext.Value.ApplyProjectEvaluationAsync(update, isActiveContext, cancellationToken);
                    }
                    else
                    {
                        await _applyChangesToWorkspaceContext.Value.ApplyProjectBuildAsync(update, isActiveContext, cancellationToken);
                    }
                }
                finally
                {
                    context.EndBatch();
                }
            }

            private void CheckForInitialized()
            {
                // We should have been initialized by our 
                // owner before they called into us
                Assumes.True(IsInitialized);

                // If we failed to create a context, we treat it as a cancellation
                if (_contextAccessor == null)
                    throw new OperationCanceledException();
            }
        }
    }
}
