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
        /// <summary>
        ///     Responsible for creating and initializing <see cref="IWorkspaceProjectContext"/> and sending 
        ///     on changes to the project to the <see cref="IApplyChangesToWorkspaceContext"/> service.
        /// </summary>
        private partial class WorkspaceContextHostInstance : AbstractMultiLifetimeInstance
        {
            private readonly ConfiguredProject _project;
            private readonly IUnconfiguredProjectCommonServices _projectServices;
            private readonly IProjectSubscriptionService _projectSubscriptionService;
            private readonly Lazy<IWorkspaceProjectContextFactory> _workspaceProjectContextFactory;
            private readonly Lazy<ISafeProjectGuidService> _projectGuidService;
            private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;
            private readonly DisposableBag _subscriptions = new DisposableBag(CancellationToken.None);

            private ExportLifetimeContext<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContext;
            private IWorkspaceProjectContext _context;

            public WorkspaceContextHostInstance(ConfiguredProject project,
                                                IUnconfiguredProjectCommonServices projectServices,
                                                IProjectSubscriptionService projectSubscriptionService, 
                                                IProjectThreadingService threadingService,
                                                Lazy<IWorkspaceProjectContextFactory> workspaceProjectContextFactory,
                                                Lazy<ISafeProjectGuidService> projectGuidService,
                                                ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _projectServices = projectServices;
                _projectSubscriptionService = projectSubscriptionService;
                _workspaceProjectContextFactory = workspaceProjectContextFactory;
                _projectGuidService = projectGuidService;
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

                _context = await CreateProjectContext(details.LanguageName, details.BinOutputPath, details.ProjectFilePath).ConfigureAwait(true);
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

                _applyChangesToWorkspaceContext.Value.ApplyDesignTime(e, true /* TODO: IsActiveContext */, DisposalToken);
            }

            private void OnEvaluationChanges(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
            {
                // TODO: Race condition can be disposed after this
                if (IsDisposing || IsDisposed)
                    return;

                _applyChangesToWorkspaceContext.Value.ApplyEvaluation(e, true /* TODO: IsActiveContext */, DisposalToken);
            }

            private async Task<IWorkspaceProjectContext> CreateProjectContext(string languageName, string binOutputPath, string projectFilePath)
            {
                Assumes.NotNull(projectFilePath);

                Guid projectGuid = await _projectGuidService.Value.GetProjectGuidAsync()
                                                                  .ConfigureAwait(true);

                Assumes.False(projectGuid == Guid.Empty);

                // If these properties (coming from MSBuild) are empty, we bail
                if (string.IsNullOrEmpty(languageName) || string.IsNullOrEmpty(binOutputPath))
                    return null;

                string workspaceProjectContextId = GetWorkspaceProjectContextId(projectFilePath, _project.ProjectConfiguration);
                object hostObject = _projectServices.Project.Services.HostObject;

                try
                {
                    return _workspaceProjectContextFactory.Value.CreateProjectContext(languageName, workspaceProjectContextId, projectFilePath, projectGuid, hostObject, binOutputPath);
                }
                catch (Exception)
                {   // TODO: Watson
                }

                return null;
            }

            private static string GetWorkspaceProjectContextId(string filePath, ProjectConfiguration projectConfiguration)
            {
                // WorkspaceContextId must be unique across the entire solution, therefore as we fire up a workspace context 
                // per implicitly active config, we factor in both the full path of the project + the name of the config.
                //
                // NOTE: Roslyn also uses this name as the default "AssemblyName" until we explicitly set it, so we need to make 
                // sure it doesn't contain any invalid path characters.
                //
                // For example:
                //      C:\Project\Project.csproj (Debug_AnyCPU)
                //      C:\Project\MultiTarget.csproj (Debug_AnyCPU_net45)

                // TODO: filePath isn't stable
                return $"{filePath} ({projectConfiguration.Name.Replace("|", "_")})";
            }
        }
    }
}
