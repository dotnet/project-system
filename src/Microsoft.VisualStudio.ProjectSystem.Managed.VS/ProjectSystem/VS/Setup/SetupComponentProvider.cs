// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Determines the VS setup component requirements of the unconfigured project and provides them
/// to <see cref="ISetupComponentRegistrationService"/> (global scope), which aggregates across
/// all projects and notifies the user to install missing components via in-product acquisition.
/// </summary>
/// <remarks>
/// Reported requirements are:
/// <list type="bullet">
///   <item><see cref="SuggestedWorkload"/> items from the <c>CollectSuggestedWorkloads</c> target (yay!).</item>
///   <item>Specific workloads based on project capabilities and hard-coded knowledge about project types and .NET features (boo!).</item>
///   <item>The .NET runtime version (for .NET Core project configurations only).</item>
/// </list>
/// Components are gathered from all active configured projects within the project.
/// </remarks>
[Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
[AppliesTo(ProjectCapability.DotNet)]
internal sealed class SetupComponentProvider : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
{
    private static readonly IImmutableSet<string> s_evaluationRuleNames = ImmutableStringHashSet.EmptyRuleNames.Add(ConfigurationGeneral.SchemaName);
    private static readonly IImmutableSet<string> s_buildRuleNames = ImmutableStringHashSet.EmptyRuleNames.Add(SuggestedWorkload.SchemaName);

    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly ISafeProjectGuidService _safeProjectGuidService;
    private readonly ISetupComponentRegistrationService _setupComponentRegistrationService;
    private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
    private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
    private readonly IProjectFaultHandlerService _projectFaultHandlerService;
    private readonly DisposableBag _disposables = new();

    private Guid _projectGuid;

    [ImportingConstructor]
    public SetupComponentProvider(
        UnconfiguredProject project,
        ISafeProjectGuidService safeProjectGuidService,
        ISetupComponentRegistrationService setupComponentRegistrationService,
        IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
        IActiveConfigurationGroupService activeConfigurationGroupService,
        IProjectFaultHandlerService projectFaultHandlerService,
        IProjectThreadingService threadingService)
        : base(threadingService.JoinableTaskContext)
    {
        _unconfiguredProject = project;
        _safeProjectGuidService = safeProjectGuidService;
        _setupComponentRegistrationService = setupComponentRegistrationService;
        _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
        _activeConfigurationGroupService = activeConfigurationGroupService;
        _projectFaultHandlerService = projectFaultHandlerService;
    }

    public Task LoadAsync()
    {
        Task task = InitializeAsync();

        // Don't block on initialization here. It doesn't need to complete before we continue here,
        // and initialization will wait on some features of the project to become available, which would
        // cause a deadlock if we waited here. We file this so any exception is reported as an NFE.
        _projectFaultHandlerService.Forget(task, _unconfiguredProject, ProjectFaultSeverity.LimitedFunctionality);

        return Task.CompletedTask;
    }

    public Task UnloadAsync()
    {
        return Task.CompletedTask;
    }

    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _projectGuid = await _safeProjectGuidService.GetProjectGuidAsync(cancellationToken);

        if (_projectGuid == Guid.Empty)
        {
            System.Diagnostics.Debug.Fail("Project GUID is empty. Setup component reporting will be disabled for this project.");
            return;
        }

        // Register this project with the aggregator.
        _disposables.Add(await _setupComponentRegistrationService.RegisterProjectAsync(_projectGuid, cancellationToken));

        // Join data across configurations into a single update.
        var joinBlock = new ConfiguredProjectDataSourceJoinBlock<ConfiguredSetupComponentSnapshot>(
            configuredProject => configuredProject.Services.ExportProvider.GetExportedValue<ConfiguredSetupComponentDataSource>(),
            JoinableFactory,
            _unconfiguredProject);

        _disposables.Add(joinBlock);

        UnconfiguredSetupComponentSnapshot? snapshot = null;

        // Combine data across all configurations.
        var mergeBlock = DataflowBlockSlim.CreateTransformManyBlock<IProjectVersionedValue<IReadOnlyCollection<ConfiguredSetupComponentSnapshot>>, UnconfiguredSetupComponentSnapshot>(
            TransformMany,
            nameFormat: $"Merge {nameof(ConfiguredSetupComponentSnapshot)} {{1}}",
            skipIntermediateInputData: true,
            skipIntermediateOutputData: true); // skip data if we fall behind

        Action<UnconfiguredSetupComponentSnapshot> action = OnUpdate;

        mergeBlock.LinkTo(
            DataflowBlockFactory.CreateActionBlock(action, _unconfiguredProject, ProjectFaultSeverity.LimitedFunctionality),
            DataflowOption.PropagateCompletion);

        joinBlock.LinkTo(mergeBlock, DataflowOption.PropagateCompletion);

        // Link a data source of active ConfiguredProjects into the join block.
        _disposables.Add(
            _activeConfigurationGroupService.ActiveConfiguredProjectGroupSource.SourceBlock.LinkTo(
                joinBlock,
                DataflowOption.PropagateCompletion));

        _disposables.Add(ProjectDataSources.JoinUpstreamDataSources(JoinableFactory, _projectFaultHandlerService, _activeConfigurationGroupService.ActiveConfiguredProjectGroupSource));

        IEnumerable<UnconfiguredSetupComponentSnapshot> TransformMany(IProjectVersionedValue<IReadOnlyCollection<ConfiguredSetupComponentSnapshot>> update)
        {
            if (UnconfiguredSetupComponentSnapshot.TryUpdate(ref snapshot, update.Value))
            {
                yield return snapshot;
            }
        }

        void OnUpdate(UnconfiguredSetupComponentSnapshot snapshot)
        {
            _setupComponentRegistrationService.SetProjectComponentSnapshot(_projectGuid, snapshot);
        }
    }

    protected override Task DisposeCoreAsync(bool initialized)
    {
        _disposables.Dispose();

        return Task.CompletedTask;
    }

    [Export(typeof(ConfiguredSetupComponentDataSource))]
    private sealed class ConfiguredSetupComponentDataSource : ChainedProjectValueDataSourceBase<ConfiguredSetupComponentSnapshot>
    {
        private readonly ConfiguredProject _configuredProject;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        public ConfiguredSetupComponentDataSource(
            ConfiguredProject configuredProject,
            IProjectSubscriptionService projectSubscriptionService)
            : base(configuredProject.UnconfiguredProject.ProjectService, synchronousDisposal: false, registerDataSource: false)
        {
            _configuredProject = configuredProject;
            _projectSubscriptionService = projectSubscriptionService;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<ConfiguredSetupComponentSnapshot>> targetBlock)
        {
            ConfiguredSetupComponentSnapshot snapshot = ConfiguredSetupComponentSnapshot.Empty;

            var transform = DataflowBlockSlim.CreateTransformBlock<
                IProjectVersionedValue<(IProjectSubscriptionUpdate EvaluationUpdate, IProjectSubscriptionUpdate BuildUpdate, IProjectCapabilitiesSnapshot Capabilities)>,
                IProjectVersionedValue<ConfiguredSetupComponentSnapshot>>(
                    Transform,
                    nameFormat: $"{nameof(ConfiguredSetupComponentDataSource)} transform {{1}}",
                    skipIntermediateInputData: false,
                    skipIntermediateOutputData: true);

            transform.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            JoinUpstreamDataSources(_projectSubscriptionService.ProjectRuleSource, _projectSubscriptionService.ProjectBuildRuleSource, _configuredProject.Capabilities);

            return ProjectDataSources.SyncLinkTo(
                _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(s_evaluationRuleNames)),
                _projectSubscriptionService.ProjectBuildRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(s_buildRuleNames)),
                _configuredProject.Capabilities.SourceBlock.SyncLinkOptions(),
                target: transform,
                linkOptions: DataflowOption.PropagateCompletion);

            IProjectVersionedValue<ConfiguredSetupComponentSnapshot> Transform(IProjectVersionedValue<(IProjectSubscriptionUpdate EvaluationUpdate, IProjectSubscriptionUpdate BuildUpdate, IProjectCapabilitiesSnapshot Capabilities)> update)
            {
                // Apply the update. Note that this may return the same instance as before, however because
                // we join the output of this block with that of other blocks, we must always return a value
                // with the latest versions.
                snapshot = snapshot.Update(update.Value.EvaluationUpdate, update.Value.BuildUpdate, update.Value.Capabilities);

                return new ProjectVersionedValue<ConfiguredSetupComponentSnapshot>(snapshot, update.DataSourceVersions);
            }
        }
    }
}
