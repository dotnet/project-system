// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Force loads the active <see cref="ConfiguredProject"/> objects so that any configured project-level
    ///     services, such as evaluation and build services, are started.
    /// </summary>
    [Export(typeof(ActiveConfiguredProjectsLoader))]

    internal class ActiveConfiguredProjectsLoader : ChainedProjectValueDataSourceBase<IEnumerable<ConfiguredProject>>
    {
        private readonly UnconfiguredProject _project;
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
        private readonly IUnconfiguredProjectTasksService _tasksService;
        private readonly ITargetBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> _targetBlock;
        private IDisposable? _subscription;

        [ImportingConstructor]
        public ActiveConfiguredProjectsLoader(UnconfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService, IUnconfiguredProjectTasksService tasksService)
            : base(containingProject: project, synchronousDisposal: false)
        {
            _project = project;
            _activeConfigurationGroupService = activeConfigurationGroupService;
            _tasksService = tasksService;
            _targetBlock = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>>(OnActiveConfigurationsChangedAsync, project, ProjectFaultSeverity.LimitedFunctionality);
        }

        [ProjectAutoLoad(ProjectLoadCheckpoint.ProjectInitialCapabilitiesEstablished)]
        // NOTE we use the language service capability here to prevent loading configurations of shared projects.
        [AppliesTo(ProjectCapability.DotNetLanguageService)]
        public Task InitializeAsync()
        {
            EnsureInitialized();
            return Task.CompletedTask;
        }

        public ITargetBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> TargetBlock => _targetBlock;

        protected override void Initialize()
        {
            _subscription = _activeConfigurationGroupService.ActiveConfigurationGroupSource.SourceBlock.LinkTo(
                target: _targetBlock,
                linkOptions: DataflowOption.PropagateCompletion);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _subscription?.Dispose();
                _targetBlock.Complete();
            }
        }

        private async Task OnActiveConfigurationsChangedAsync(IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>> e)
        {
            await GetLoadedProjectsAsync(e);
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<IEnumerable<ConfiguredProject>>> targetBlock)
        {
            IReceivableSourceBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> sourceBlock = _activeConfigurationGroupService.ActiveConfigurationGroupSource.SourceBlock;

            DisposableValue<ISourceBlock<IProjectVersionedValue<IEnumerable<ConfiguredProject>>>>? transformBlock = sourceBlock.TransformWithNoDelta(TransformAsync);

            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return transformBlock;
        }

        private async Task<IProjectVersionedValue<IEnumerable<ConfiguredProject>>> TransformAsync(IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>> projectVersionedValue)
        {
            List<ConfiguredProject> generatedResult = await GetLoadedProjectsAsync(projectVersionedValue);

            return new ProjectVersionedValue<IEnumerable<ConfiguredProject>>(generatedResult, projectVersionedValue.DataSourceVersions);
        }

        private async Task<List<ConfiguredProject>> GetLoadedProjectsAsync(IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>> projectVersionedValue)
        {
            List<ConfiguredProject> generatedResult = new List<ConfiguredProject>();

            foreach (ProjectConfiguration configuration in projectVersionedValue.Value)
            {
                // Make sure we aren't currently unloading, or we don't unload while we load the configuration
                var loadedConfiguredProject = await _tasksService.LoadedProjectAsync(() =>
                {
                    return _project.LoadConfiguredProjectAsync(configuration);
                });

                generatedResult.Add(loadedConfiguredProject);
            }

            return generatedResult;
        }
    }
}
