// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Force loads the active <see cref="ConfiguredProject"/> objects so that any configured project-level
    ///     services, such as evaluation and build services, are started.
    /// </summary>

    internal class ActiveConfiguredProjectsLoader : OnceInitializedOnceDisposed
    {
        private readonly UnconfiguredProject _project;
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
        private readonly IUnconfiguredProjectTasksService _tasksService;
        private readonly ITargetBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> _targetBlock;
        private IDisposable? _subscription;

        [ImportingConstructor]
        public ActiveConfiguredProjectsLoader(UnconfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService, IUnconfiguredProjectTasksService tasksService)
        {
            _project = project;
            _activeConfigurationGroupService = activeConfigurationGroupService;
            _tasksService = tasksService;
            _targetBlock = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>>(OnActiveConfigurationsChangedAsync, project, ProjectFaultSeverity.LimitedFunctionality);
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectInitialCapabilitiesEstablished)]
        // NOTE we use the language service capability here to prevent loading configurations of shared projects.
        [AppliesTo(ProjectCapability.DotNetLanguageService)]
        public Task InitializeAsync()
        {
            EnsureInitialized();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Exposed for unit testing only.
        /// </summary>
        internal ITargetBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> TargetBlock => _targetBlock;

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
            foreach (ProjectConfiguration configuration in e.Value)
            {
                // Make sure we aren't currently unloading, or we don't unload while we load the configuration
                await _tasksService.LoadedProjectAsync(() =>
                {
                    return _project.LoadConfiguredProjectAsync(configuration);
                });
            }
        }
    }
}
