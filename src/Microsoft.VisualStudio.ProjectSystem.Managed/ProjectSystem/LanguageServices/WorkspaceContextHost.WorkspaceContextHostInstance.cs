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
            private IWorkspaceProjectContext _context;
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
                _context = await _workspaceProjectContextProvider.CreateProjectContextAsync(_project)
                                                                  .ConfigureAwait(true);

                if (_context == null)
                    return;

                _applyChangesToWorkspaceContext = _applyChangesToWorkspaceContextFactory.CreateExport();
                _applyChangesToWorkspaceContext.Value.Initialize(_context);

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

                    if (_context != null)
                    {
                        await _workspaceProjectContextProvider.ReleaseProjectContextAsync(_context)
                                                              .ConfigureAwait(true);
                    }
                }
            }

            public async Task OpenContextForWriteAsync(Func<IWorkspaceProjectContext, Task> action)
            {
                await InitializationCompletion;

                // If we failed to create a context, we treat it as a cancellation
                if (_context == null)
                    throw new OperationCanceledException();

                CancellationToken cancellationToken = _tasksService.UnloadCancellationToken;

                // TODO: https://github.com/dotnet/project-system/issues/353
                await _threadingService.SwitchToUIThread(cancellationToken);

                await ExecuteUnderLockAsync(_ => action(_context), cancellationToken);
            }

            internal async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool evaluation)
            {
                CancellationToken cancellationToken = _tasksService.UnloadCancellationToken;

                // TODO: https://github.com/dotnet/project-system/issues/353
                await _threadingService.SwitchToUIThread(cancellationToken);

                await ExecuteUnderLockAsync(ct =>
                {
                    ApplyProjectChangesUnderLock(update, evaluation, ct);

                    return Task.CompletedTask;
                }, cancellationToken);
            }

            private void ApplyProjectChangesUnderLock(IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool evaluation, CancellationToken cancellationToken)
            {
                bool isActiveContext = _activeWorkspaceProjectContextTracker.IsActiveContext(_context);

                if (evaluation)
                {
                    _applyChangesToWorkspaceContext.Value.ApplyProjectEvaluation(update, isActiveContext, cancellationToken);
                }
                else
                {
                    _applyChangesToWorkspaceContext.Value.ApplyProjectBuild(update, isActiveContext, cancellationToken);
                }
            }
        }
    }
}
