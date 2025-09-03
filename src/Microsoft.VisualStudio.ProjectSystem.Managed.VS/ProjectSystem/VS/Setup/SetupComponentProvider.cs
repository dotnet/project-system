// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
///   <item><see cref="SuggestedVisualStudioComponentId"/> items from the <c>CollectSuggestedVisualStudioComponentIds</c> target (yay!).</item>
///   <item>Specific workloads based on project capabilities and hard-coded knowledge about project types and .NET features (boo!).</item>
///   <item>The .NET runtime version (for .NET Core project configurations only).</item>
/// </list>
/// Components are gathered from all active configured projects within the project.
/// </remarks>
[Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
[AppliesTo(ProjectCapability.DotNet)]
[ExportInitialBuildRulesSubscriptions(SuggestedVisualStudioComponentId.SchemaName)]
[ProjectDynamicLoadComponent(ProjectLoadCheckpoint.ProjectBackgroundLoadCompleted)]
internal sealed class SetupComponentProvider : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
{
    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly ISafeProjectGuidService _safeProjectGuidService;
    private readonly ISetupComponentRegistrationService _setupComponentRegistrationService;
    private readonly UnconfiguredSetupComponentDataSource _unconfiguredSetupComponentDataSource;
    private readonly IProjectFaultHandlerService _projectFaultHandlerService;
    private readonly DisposableBag _disposables = [];

    private Guid _projectGuid;

    [ImportingConstructor]
    public SetupComponentProvider(
        UnconfiguredProject project,
        ISafeProjectGuidService safeProjectGuidService,
        ISetupComponentRegistrationService setupComponentRegistrationService,
        UnconfiguredSetupComponentDataSource unconfiguredSetupComponentDataSource,
        IProjectFaultHandlerService projectFaultHandlerService,
        IProjectThreadingService threadingService)
        : base(threadingService.JoinableTaskContext)
    {
        _unconfiguredProject = project;
        _safeProjectGuidService = safeProjectGuidService;
        _setupComponentRegistrationService = setupComponentRegistrationService;
        _unconfiguredSetupComponentDataSource = unconfiguredSetupComponentDataSource;
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

        Action<IProjectVersionedValue<UnconfiguredSetupComponentSnapshot>> action = OnUpdate;

        _unconfiguredSetupComponentDataSource.SourceBlock.LinkTo(
            DataflowBlockFactory.CreateActionBlock(action, _unconfiguredProject, ProjectFaultSeverity.LimitedFunctionality),
            DataflowOption.PropagateCompletion);

        void OnUpdate(IProjectVersionedValue<UnconfiguredSetupComponentSnapshot> snapshot)
        {
            _setupComponentRegistrationService.SetProjectComponentSnapshot(_projectGuid, snapshot.Value);
        }
    }

    protected override Task DisposeCoreAsync(bool initialized)
    {
        _disposables.Dispose();

        return Task.CompletedTask;
    }
}
