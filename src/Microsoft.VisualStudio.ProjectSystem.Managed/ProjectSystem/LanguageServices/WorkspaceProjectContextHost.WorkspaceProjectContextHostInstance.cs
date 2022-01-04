// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.OperationProgress;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceProjectContextHost
    {
        /// <summary>
        ///     Responsible for lifetime of a <see cref="IWorkspaceProjectContext"/> and applying changes to a
        ///     project to the context via the <see cref="IApplyChangesToWorkspaceContext"/> service.
        /// </summary>
        internal partial class WorkspaceProjectContextHostInstance : OnceInitializedOnceDisposedUnderLockAsync, IMultiLifetimeInstance
        {
            private readonly ConfiguredProject _project;
            private readonly IProjectSubscriptionService _projectSubscriptionService;
            private readonly IUnconfiguredProjectTasksService _tasksService;
            private readonly IWorkspaceProjectContextProvider _workspaceProjectContextProvider;
            private readonly IActiveEditorContextTracker _activeWorkspaceProjectContextTracker;
            private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;
            private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;
            private readonly IDataProgressTrackerService _dataProgressTrackerService;
            private readonly ICommandLineArgumentsProvider _commandLineArgumentsProvider;

            private IDataProgressTrackerServiceRegistration? _evaluationProgressRegistration;
            private IDataProgressTrackerServiceRegistration? _projectBuildProgressRegistration;
            private IDataProgressTrackerServiceRegistration? _sourceItemsProgressRegistration;
            private DisposableBag? _disposables;
            private IWorkspaceProjectContextAccessor? _contextAccessor;
            private ExportLifetimeContext<IApplyChangesToWorkspaceContext>? _applyChangesToWorkspaceContext;
            private ContextState? _lastContextState = null;

            public WorkspaceProjectContextHostInstance(ConfiguredProject project,
                                                       IProjectThreadingService threadingService,
                                                       IUnconfiguredProjectTasksService tasksService,
                                                       IProjectSubscriptionService projectSubscriptionService,
                                                       IWorkspaceProjectContextProvider workspaceProjectContextProvider,
                                                       IActiveEditorContextTracker activeWorkspaceProjectContextTracker,
                                                       IActiveConfiguredProjectProvider activeConfiguredProjectProvider,
                                                       ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory,
                                                       IDataProgressTrackerService dataProgressTrackerService,
                                                       ICommandLineArgumentsProvider commandLineArgumentsProvider)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _projectSubscriptionService = projectSubscriptionService;
                _tasksService = tasksService;
                _workspaceProjectContextProvider = workspaceProjectContextProvider;
                _activeWorkspaceProjectContextTracker = activeWorkspaceProjectContextTracker;
                _activeConfiguredProjectProvider = activeConfiguredProjectProvider;
                _applyChangesToWorkspaceContextFactory = applyChangesToWorkspaceContextFactory;
                _dataProgressTrackerService = dataProgressTrackerService;
                _commandLineArgumentsProvider = commandLineArgumentsProvider;
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

                _activeWorkspaceProjectContextTracker.RegisterContext(_contextAccessor.ContextId);

                _applyChangesToWorkspaceContext = _applyChangesToWorkspaceContextFactory.CreateExport();
                _applyChangesToWorkspaceContext.Value.Initialize(_contextAccessor.Context);

                _evaluationProgressRegistration = _dataProgressTrackerService.RegisterForIntelliSense(this, _project, nameof(WorkspaceProjectContextHostInstance) + ".Evaluation");
                _projectBuildProgressRegistration = _dataProgressTrackerService.RegisterForIntelliSense(this, _project, nameof(WorkspaceProjectContextHostInstance) + ".ProjectBuild");
                _sourceItemsProgressRegistration = _dataProgressTrackerService.RegisterForIntelliSense(this, _project, nameof(WorkspaceProjectContextHostInstance) + ".SourceItems");

                _disposables = new DisposableBag
                {
                    _applyChangesToWorkspaceContext,
                    _evaluationProgressRegistration,
                    _projectBuildProgressRegistration,
                    _sourceItemsProgressRegistration,

                    ProjectDataSources.SyncLinkTo(
                        _activeConfiguredProjectProvider.ActiveConfiguredProjectBlock.SyncLinkOptions(),
                        _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(GetProjectEvaluationOptions()),
                        target: DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<(ConfiguredProject, IProjectSubscriptionUpdate)>>(
                            e => OnProjectChangedAsync(new ProjectChange(e), WorkspaceContextHandlerType.Evaluation),
                            _project.UnconfiguredProject,
                            ProjectFaultSeverity.LimitedFunctionality),
                        linkOptions: DataflowOption.PropagateCompletion,
                        cancellationToken: cancellationToken),

                    ProjectDataSources.SyncLinkTo(
                        _activeConfiguredProjectProvider.ActiveConfiguredProjectBlock.SyncLinkOptions(),
                        _projectSubscriptionService.ProjectBuildRuleSource.SourceBlock.SyncLinkOptions(GetProjectBuildOptions()),
                        _commandLineArgumentsProvider.SourceBlock.SyncLinkOptions(),
                        target: DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<(ConfiguredProject, IProjectSubscriptionUpdate, CommandLineArgumentsSnapshot)>>(
                            e => OnProjectChangedAsync(new ProjectChange(e), WorkspaceContextHandlerType.ProjectBuild),
                            _project.UnconfiguredProject,
                            ProjectFaultSeverity.LimitedFunctionality),
                        linkOptions: DataflowOption.PropagateCompletion,
                        cancellationToken: cancellationToken),

                    ProjectDataSources.SyncLinkTo(
                        _activeConfiguredProjectProvider.ActiveConfiguredProjectBlock.SyncLinkOptions(),
                        _projectSubscriptionService.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                        target: DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<(ConfiguredProject, IProjectSubscriptionUpdate)>>(
                            e => OnProjectChangedAsync(new ProjectChange(e), WorkspaceContextHandlerType.SourceItems),
                            _project.UnconfiguredProject,
                            ProjectFaultSeverity.LimitedFunctionality),
                        linkOptions: DataflowOption.PropagateCompletion,
                        cancellationToken: cancellationToken),
                };

                return;

                StandardRuleDataflowLinkOptions GetProjectEvaluationOptions()
                {
                    return DataflowOption.WithRuleNames(_applyChangesToWorkspaceContext.Value.GetProjectEvaluationRules());
                }

                StandardRuleDataflowLinkOptions GetProjectBuildOptions()
                {
                    return DataflowOption.WithRuleNames(_applyChangesToWorkspaceContext.Value.GetProjectBuildRules());
                }
            }

            protected override async Task DisposeCoreUnderLockAsync(bool initialized)
            {
                if (initialized)
                {
                    _disposables?.Dispose();

                    if (_contextAccessor != null)
                    {
                        _activeWorkspaceProjectContextTracker.UnregisterContext(_contextAccessor.ContextId);

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

            internal Task OnProjectChangedAsync(ProjectChange change, WorkspaceContextHandlerType handlerType)
            {
                return ExecuteUnderLockAsync(ct => ApplyProjectChangesUnderLockAsync(change, handlerType, ct), _tasksService.UnloadCancellationToken);
            }

            private async Task ApplyProjectChangesUnderLockAsync(ProjectChange change, WorkspaceContextHandlerType handlerType, CancellationToken cancellationToken)
            {
                // NOTE we cannot call CheckForInitialized here, as this method may be invoked during initialization
                Assumes.NotNull(_contextAccessor);

                bool isActiveEditorContext = _activeWorkspaceProjectContextTracker.IsActiveEditorContext(_contextAccessor.ContextId);
                bool isActiveConfiguration = change.ActiveConfiguredProject == _project;

                bool hasChange =
                    change.Subscription.Value.ProjectChanges.Any(c => c.Value.Difference.AnyChanges) ||
                    change.CommandLineArgumentsSnapshot?.IsChanged == true ||
                    _lastContextState?.IsActiveConfiguration != isActiveConfiguration ||
                    _lastContextState?.IsActiveEditorContext != isActiveEditorContext;

                // Avoid starting a batch when nothing has actually changed
                if (!hasChange)
                {
                    return;
                }

                var state = new ContextState(isActiveEditorContext, isActiveConfiguration);

                _lastContextState = state;

                IWorkspaceProjectContext context = _contextAccessor.Context;

                context.StartBatch();

                try
                {
                    switch (handlerType)
                    {
                        case WorkspaceContextHandlerType.Evaluation:
                            _applyChangesToWorkspaceContext!.Value.ApplyProjectEvaluation(change.Subscription, state, cancellationToken);
                            break;

                        case WorkspaceContextHandlerType.ProjectBuild:
                            Assumes.NotNull(change.CommandLineArgumentsSnapshot);
                            _applyChangesToWorkspaceContext!.Value.ApplyProjectBuild(change.Subscription, change.CommandLineArgumentsSnapshot, state, cancellationToken);
                            break;

                        case WorkspaceContextHandlerType.SourceItems:
                            _applyChangesToWorkspaceContext!.Value.ApplySourceItems(change.Subscription, state, cancellationToken);
                            break;
                    }
                }
                finally
                {
                    await context.EndBatchAsync();

                    // Notify operation progress that we've now processed these versions of our input, if they are
                    // up-to-date with the latest version that produced, then we no longer considered "in progress".
                    IDataProgressTrackerServiceRegistration? registration = handlerType switch
                    {
                        WorkspaceContextHandlerType.Evaluation => _evaluationProgressRegistration,
                        WorkspaceContextHandlerType.ProjectBuild => _projectBuildProgressRegistration,
                        WorkspaceContextHandlerType.SourceItems => _sourceItemsProgressRegistration,
                        _ => throw new NotImplementedException()
                    };

                    registration!.NotifyOutputDataCalculated(change.DataSourceVersions);
                }
            }

            [MemberNotNull(nameof(_contextAccessor))]
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
