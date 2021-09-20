// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

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
        private readonly IProjectFaultHandlerService _faultHandlerService;
        private readonly ISafeProjectGuidService _projectGuidService;
        private readonly Lazy<IWorkspaceProjectContextFactory> _workspaceProjectContextFactory;

        [ImportingConstructor]
        public WorkspaceProjectContextProvider(UnconfiguredProject project,
                                               ISafeProjectGuidService projectGuidService,
                                               IProjectFaultHandlerService faultHandlerService,
                                               Lazy<IWorkspaceProjectContextFactory> workspaceProjectContextFactory)        // From Roslyn, so lazy
        {
            _project = project;
            _faultHandlerService = faultHandlerService;
            _workspaceProjectContextFactory = workspaceProjectContextFactory;
            _projectGuidService = projectGuidService;
        }

        public async Task<IWorkspaceProjectContextAccessor?> CreateProjectContextAsync(ConfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            ProjectContextInitData data = await GetProjectContextInitDataAsync(project);
            if (data.IsInvalid())
                return null;

            object? hostObject = _project.Services.HostObject;

            IWorkspaceProjectContext? context = await CreateProjectContextHandlingFaultAsync(data, hostObject);
            if (context == null)
                return null;

            return new WorkspaceProjectContextAccessor(data.WorkspaceProjectContextId, context);
        }

        public async Task ReleaseProjectContextAsync(IWorkspaceProjectContextAccessor accessor)
        {
            Requires.NotNull(accessor, nameof(accessor));

            try
            {
                accessor.Context.Dispose();
            }
            catch (Exception ex)
            {
                await _faultHandlerService.ReportFaultAsync(ex, _project, ProjectFaultSeverity.Recoverable);
            }
        }

        private async Task<IWorkspaceProjectContext?> CreateProjectContextHandlingFaultAsync(ProjectContextInitData data, object? hostObject)
        {
            try
            {
                // Call into Roslyn to init language service for this project
                IWorkspaceProjectContext context = await _workspaceProjectContextFactory.Value.CreateProjectContextAsync(
                                                                                    data.LanguageName,
                                                                                    data.WorkspaceProjectContextId,
                                                                                    data.ProjectFilePath,
                                                                                    data.ProjectGuid,
                                                                                    hostObject,
                                                                                    data.BinOutputPath,
                                                                                    data.AssemblyName,
                                                                                    CancellationToken.None);

                context.LastDesignTimeBuildSucceeded = false;  // By default, turn off diagnostics until the first design time build succeeds for this project.

                return context;
            }
            catch (Exception ex)
            {
                await _faultHandlerService.ReportFaultAsync(ex, _project, ProjectFaultSeverity.LimitedFunctionality);
            }

            return null;
        }

        private async Task<ProjectContextInitData> GetProjectContextInitDataAsync(ConfiguredProject project)
        {
            Guid projectGuid = await _projectGuidService.GetProjectGuidAsync();

            IProjectRuleSnapshot snapshot = await GetLatestSnapshotAsync(project);

            return ProjectContextInitData.GetProjectContextInitData(snapshot, projectGuid, project.ProjectConfiguration);
        }

        protected virtual async Task<IProjectRuleSnapshot> GetLatestSnapshotAsync(ConfiguredProject project)
        {
            IProjectSubscriptionService? service = project.Services.ProjectSubscription;
            Assumes.Present(service);

            IImmutableDictionary<string, IProjectRuleSnapshot> update = await service.ProjectRuleSource.GetLatestVersionAsync(project, new string[] { ConfigurationGeneral.SchemaName });

            return update[ConfigurationGeneral.SchemaName];
        }
    }
}
