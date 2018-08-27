// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Creates <see cref="IWorkspaceProjectContext"/> instances using data 
    ///     from any specified <see cref="ConfiguredProject"/>.
    /// </summary>
    [Export(typeof(IWorkspaceProjectContextProvider))]
    internal partial class WorkspaceProjectContextProvider : IWorkspaceProjectContextProvider
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectThreadingService _threadingService;
        private readonly ITelemetryService _telemetryService;
        private readonly ISafeProjectGuidService _projectGuidService;
        private readonly Lazy<IWorkspaceProjectContextFactory> _workspaceProjectContextFactory;

        [ImportingConstructor]
        public WorkspaceProjectContextProvider(UnconfiguredProject project,
                                               IProjectThreadingService threadingService,
                                               ISafeProjectGuidService projectGuidService,
                                               ITelemetryService telemetryService,
                                               Lazy<IWorkspaceProjectContextFactory> workspaceProjectContextFactory)
        {
            _project = project;
            _threadingService = threadingService;
            _telemetryService = telemetryService;
            _workspaceProjectContextFactory = workspaceProjectContextFactory;
            _projectGuidService = projectGuidService;
        }

        public async Task<IWorkspaceProjectContext> CreateProjectContextAsync(ConfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            ProjectContextInitData data = await GetProjectContextInitDataAsync(project).ConfigureAwait(true);
            if (data.IsInvalid())
                return null;

            object hostObject = _project.Services.HostObject;

            IWorkspaceProjectContext context = await CreateProjectContextHandlingFaultAsync(data, hostObject).ConfigureAwait(true);
            if (context == null)
                return null;

            // Wrap to enforce UI-thread
            return new ForegroundWorkspaceProjectContext(_threadingService, context);
        }

        public async Task ReleaseProjectContextAsync(IWorkspaceProjectContext projectContext)
        {
            Requires.NotNull(projectContext, nameof(projectContext));

            await _threadingService.SwitchToUIThread();

            try
            {
                projectContext.Dispose();
            }
            catch (Exception ex) when(_telemetryService.PostFault(TelemetryEventName.LanguageServiceInitFault, ex))
            {
            }
        }

        private async Task<IWorkspaceProjectContext> CreateProjectContextHandlingFaultAsync(ProjectContextInitData data, object hostObject)
        {
            await _threadingService.SwitchToUIThread();

            try
            {
                // Call into Roslyn to init language service for this project
                IWorkspaceProjectContext context = _workspaceProjectContextFactory.Value.CreateProjectContext(
                                                                                    data.LanguageName, 
                                                                                    data.WorkspaceProjectContextId, 
                                                                                    data.ProjectFilePath, 
                                                                                    data.ProjectGuid, 
                                                                                    hostObject, 
                                                                                    data.BinOutputPath);

                context.LastDesignTimeBuildSucceeded = false;  // By default, turn off diagnostics until the first design time build succeeds for this project.

                return context;
            }
            catch (Exception ex) when (_telemetryService.PostFault(TelemetryEventName.LanguageServiceInitFault, ex))
            {   
            }

            return null;
        }

        private async Task<ProjectContextInitData> GetProjectContextInitDataAsync(ConfiguredProject project)
        {
            Guid projectGuid = await _projectGuidService.GetProjectGuidAsync()
                                                        .ConfigureAwait(true);

            IProjectRuleSnapshot snapshot = await GetLatestSnapshotAsync(project).ConfigureAwait(true);

            return ProjectContextInitData.GetProjectContextInitData(snapshot, projectGuid, project.ProjectConfiguration);
        }

        protected virtual async Task<IProjectRuleSnapshot> GetLatestSnapshotAsync(ConfiguredProject project)
        {
            IProjectSubscriptionService service = project.Services.ProjectSubscription;

            IImmutableDictionary<string, IProjectRuleSnapshot> update = await service.ProjectRuleSource.GetLatestVersionAsync(project, new string[] { ConfigurationGeneral.SchemaName })
                                                                                                       .ConfigureAwait(true);

            return update[ConfigurationGeneral.SchemaName];
        }
    }
}
