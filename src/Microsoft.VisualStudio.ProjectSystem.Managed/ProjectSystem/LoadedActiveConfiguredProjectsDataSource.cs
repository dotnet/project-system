// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem;

/// <summary>
///     Force loads the active <see cref="ConfiguredProject"/> objects so that any configured project-level
///     services, such as evaluation and build services, are started.
/// </summary>
[Export(typeof(ILoadedActiveConfiguredProjectDataSource))]
internal class LoadedActiveConfiguredProjectsDataSource : ChainedProjectValueDataSourceBase<IEnumerable<ConfiguredProject>>, ILoadedActiveConfiguredProjectDataSource
{
    private readonly UnconfiguredProject _project;
    private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
    private readonly IUnconfiguredProjectTasksService _tasksService;

    [ImportingConstructor]
    public LoadedActiveConfiguredProjectsDataSource(UnconfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService, IUnconfiguredProjectTasksService tasksService)
        : base(containingProject: project, synchronousDisposal: false)
    {
        _project = project;
        _activeConfigurationGroupService = activeConfigurationGroupService;
        _tasksService = tasksService;
    }

    protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<IEnumerable<ConfiguredProject>>> targetBlock)
    {
        IReceivableSourceBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> sourceBlock = _activeConfigurationGroupService.ActiveConfigurationGroupSource.SourceBlock;

        DisposableValue<ISourceBlock<IProjectVersionedValue<IEnumerable<ConfiguredProject>>>>? transformBlock = sourceBlock.TransformWithNoDelta(TransformAsync);

        var link = transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

        JoinUpstreamDataSources(_activeConfigurationGroupService.ActiveConfigurationGroupSource);

        return link;
    }

    private async Task<IProjectVersionedValue<IEnumerable<ConfiguredProject>>> TransformAsync(IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>> projectVersionedValue)
    {
        List<ConfiguredProject> generatedResult = await GetLoadedProjectsAsync(projectVersionedValue);

        return new ProjectVersionedValue<IEnumerable<ConfiguredProject>>(generatedResult, projectVersionedValue.DataSourceVersions);
    }

    private async Task<List<ConfiguredProject>> GetLoadedProjectsAsync(IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>> projectVersionedValue)
    {
        return await JoinableFactory.RunAsync(async () =>
        {
            List<ConfiguredProject> generatedResult = new List<ConfiguredProject>();

            foreach (ProjectConfiguration configuration in projectVersionedValue.Value)
            {
                // Make sure we aren't currently unloading, or we don't unload while we load the configuration
                var loadedConfiguredProject = await _tasksService.LoadedProjectAsync(() => _project.LoadConfiguredProjectAsync(configuration));

                generatedResult.Add(loadedConfiguredProject);
            }

            return generatedResult;
        });
    }
}
