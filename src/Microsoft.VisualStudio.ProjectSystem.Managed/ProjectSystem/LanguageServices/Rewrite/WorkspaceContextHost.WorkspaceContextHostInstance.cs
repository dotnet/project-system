// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceContextHost
    {
        private partial class WorkspaceContextHostInstance : AbstractProjectDynamicLoadInstance
        {
            private readonly ConfiguredProject _project;
            private readonly IProjectSubscriptionService _projectSubscriptionService;
            private readonly Lazy<WorkspaceProjectContextCreator> _workspaceProjectContextCreator;
            private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;
            private readonly DisposableBag _subscriptions = new DisposableBag(CancellationToken.None);

            private ExportLifetimeContext<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContext;
            private IWorkspaceProjectContext _context;
            

            public WorkspaceContextHostInstance(ConfiguredProject project,
                                                IProjectSubscriptionService projectSubscriptionService, 
                                                IProjectThreadingService threadingService,
                                                Lazy<WorkspaceProjectContextCreator> workspaceProjectContextCreator,
                                                ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _projectSubscriptionService = projectSubscriptionService;
                _workspaceProjectContextCreator = workspaceProjectContextCreator;
                _applyChangesToWorkspaceContextFactory = applyChangesToWorkspaceContextFactory;
            }

            internal async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
            {
                if (IsDisposing || IsDisposed)
                    return;

                var details = ProjectData.FromSnapshot(_project, e.Value.CurrentState[ConfigurationGeneral.SchemaName]);

                await InitializeProjectContext(details).ConfigureAwait(true);

                _context.BinOutputPath = details.BinOutputPath;
                _context.DisplayName = details.DisplayName;
                _context.ProjectFilePath = details.ProjectFilePath;
            }

            private async Task InitializeProjectContext(ProjectData details)
            {
                if (_context != null)
                    return;

                _context = await _workspaceProjectContextCreator.Value.CreateProjectContext(details.LanguageName, details.BinOutputPath, details.ProjectFilePath)
                                                                      .ConfigureAwait(true);

                _applyChangesToWorkspaceContext = _applyChangesToWorkspaceContextFactory.CreateExport();
                _applyChangesToWorkspaceContext.Value.Initialize(_context);

                _subscriptions.AddDisposable(_project.Services.ProjectSubscription.JointRuleSource.SourceBlock.LinkTo(
                    new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnDesignTimeChanges(e)),
                        ruleNames: _applyChangesToWorkspaceContext.Value.GetDesignTimeRules(), suppressVersionOnlyUpdates: true));

                _subscriptions.AddDisposable(_project.Services.ProjectSubscription.ProjectRuleSource.SourceBlock.LinkTo(
                    new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnEvaluationChanges(e)),
                    ruleNames: _applyChangesToWorkspaceContext.Value.GetEvaluationRules(), suppressVersionOnlyUpdates: true));
            }

            protected override Task DisposeCoreAsync(bool initialized)
            {
                _subscriptions.Dispose();
                _applyChangesToWorkspaceContext?.Dispose();
                _context?.Dispose();

                return Task.CompletedTask;
            }

            protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                _subscriptions.AddDisposable(_projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    target: new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(OnProjectChangedAsync),
                    ruleNames: ConfigurationGeneral.SchemaName,
                    suppressVersionOnlyUpdates: true,
                    linkOptions: new DataflowLinkOptions() { PropagateCompletion = true }));

                return Task.CompletedTask;
            }

            private void OnDesignTimeChanges(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
            {
                // TODO: Race condition can be disposed after this
                if (IsDisposing || IsDisposed)
                    return;

                _applyChangesToWorkspaceContext.Value.ApplyDesignTime(e, true /* TODO: IsActiveContext */);
            }

            private void OnEvaluationChanges(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
            {
                // TODO: Race condition can be disposed after this
                if (IsDisposing || IsDisposed)
                    return;

                _applyChangesToWorkspaceContext.Value.ApplyEvaluation(e, true /* TODO: IsActiveContext */);
            }
        }
    }
}
