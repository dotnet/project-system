// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    partial class WorkspaceContextHost
    {
        private class WorkspaceContextHostInstance : AbstractProjectDynamicLoadInstance
        {
            private readonly ConfiguredProject _project;
            private readonly IUnconfiguredProjectCommonServices _projectServices;
            private readonly IProjectSubscriptionService _projectSubscriptionService;
            private readonly ISafeProjectGuidService _projectGuidService;
            private readonly Lazy<IWorkspaceProjectContextFactory> _workspaceProjectContextFactory;
            private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;

            private ExportLifetimeContext<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContext;
            private IWorkspaceProjectContext _context;
            private DisposableBag _subscriptions = new DisposableBag(CancellationToken.None);

            public WorkspaceContextHostInstance(ConfiguredProject project,
                                                IUnconfiguredProjectCommonServices projectServices, 
                                                IProjectSubscriptionService projectSubscriptionService, 
                                                ISafeProjectGuidService projectGuidService,
                                                Lazy<IWorkspaceProjectContextFactory> workspaceProjectContextFactory,
                                                ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory)
                : base(projectServices.ThreadingService.JoinableTaskContext)
            {
                _projectGuidService = projectGuidService;
                _project = project;
                _projectServices = projectServices;
                _projectSubscriptionService = projectSubscriptionService;
                _workspaceProjectContextFactory = workspaceProjectContextFactory;
                _applyChangesToWorkspaceContextFactory = applyChangesToWorkspaceContextFactory;
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

            internal async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
            {
                if (IsDisposing || IsDisposed)
                    return;

                var details = ProjectData.FromSnapshot(e.Value.CurrentState[ConfigurationGeneral.SchemaName]);

                await InitializeProjectContext(details).ConfigureAwait(false);

                // Was the project setup correctly?
                if (_context == null)
                    return;

                _context.BinOutputPath = details.BinOutputPath;
                _context.DisplayName = details.DisplayName;
                _context.ProjectFilePath = details.ProjectFilePath;
            }

            private async Task InitializeProjectContext(ProjectData details)
            {
                if (_context == null)
                {
                    _context = await CreateProjectContextAsync(details).ConfigureAwait(false);

                    if (_context == null)
                        return;
                    
                    _applyChangesToWorkspaceContext = _applyChangesToWorkspaceContextFactory.CreateExport();
                    _applyChangesToWorkspaceContext.Value.Initialize(_context);

                    _subscriptions.AddDisposable(_project.Services.ProjectSubscription.JointRuleSource.SourceBlock.LinkTo(
                        new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnDesignTimeChanges(e)),
                         ruleNames: _applyChangesToWorkspaceContext.Value.GetDesignTimeRules(), suppressVersionOnlyUpdates: true));

                    _subscriptions.AddDisposable(_project.Services.ProjectSubscription.ProjectRuleSource.SourceBlock.LinkTo(
                        new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnEvaluationChanges(e)),
                        ruleNames: _applyChangesToWorkspaceContext.Value.GetEvaluationRules(), suppressVersionOnlyUpdates: true));
                }
            }

            private void OnDesignTimeChanges(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
            {
                _applyChangesToWorkspaceContext.Value.ApplyDesignTime(e, true /* TODO: IsActiveContext */);
            }

            private void OnEvaluationChanges(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
            {
                _applyChangesToWorkspaceContext.Value.ApplyEvaluation(e, true /* TODO: IsActiveContext */);
            }

            private async Task<IWorkspaceProjectContext> CreateProjectContextAsync(ProjectData details)
            {
                // Bail if our project doesn't have the right properties set
                if (string.IsNullOrEmpty(details.LanguageName) || string.IsNullOrEmpty(details.BinOutputPath))
                    return null;

                Guid projectGuid = await _projectGuidService.GetProjectGuidAsync()
                                                            .ConfigureAwait(false);

                Assumes.False(projectGuid == Guid.Empty);
                
                // Fire up the Roslyn "project"
                return _workspaceProjectContextFactory.Value.CreateProjectContext(details.LanguageName, details.DisplayName, details.ProjectFilePath, projectGuid, _projectServices.Project.Services.HostObject, details.BinOutputPath);
            }

            private struct ProjectData
            {
                public string LanguageName;
                public string BinOutputPath;
                public string DisplayName;
                public string ProjectFilePath;

                public static ProjectData FromSnapshot(IProjectRuleSnapshot snapshot)
                {
                    var data = new ProjectData();

                    snapshot.Properties.TryGetValue(ConfigurationGeneral.LanguageServiceNameProperty, out data.LanguageName);
                    snapshot.Properties.TryGetValue(ConfigurationGeneral.TargetPathProperty, out data.BinOutputPath);
                    snapshot.Properties.TryGetValue(ConfigurationGeneral.MSBuildProjectFullPathProperty, out data.ProjectFilePath);

                    data.DisplayName = GetDisplayName(data.ProjectFilePath);

                    return data;
                }

                public static string GetDisplayName(string filePath)
                {
                    string displayName = Path.GetFileNameWithoutExtension(filePath);

                    // TODO: Multi-targeting
                    return displayName;
                }
            }
        }
    }
}
