// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Determines the VS setup component requirements of the configured project and provides them
/// to <see cref="ISetupComponentRegistrationService"/> (global scope), which aggregates across
/// all projects and notifies the user to install missing components via in-product acquisition.
/// </summary>
/// <remarks>
/// Reported requirements are:
/// <list type="bullet">
///   <item><see cref="SuggestedWorkload"/> items from the <c>CollectSuggestedWorkloads</c> target (yay!).</item>
///   <item>Specific workloads based on project capabilities and hard-coded knowledge about project types and .NET features (boo!).</item>
///   <item>The .NET runtime version (for .NET Core project configurations).</item>
/// </list>
/// </remarks>
[Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
[AppliesTo(ProjectCapability.DotNet)]
internal sealed class SetupComponentProvider : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
{
    private static readonly IImmutableSet<string> s_evaluationRuleNames = ImmutableHashSet<string>.Empty.WithComparer(StringComparers.RuleNames).Add(ConfigurationGeneral.SchemaName);
    private static readonly IImmutableSet<string> s_buildRuleNames = ImmutableHashSet<string>.Empty.WithComparer(StringComparers.RuleNames).Add(SuggestedWorkload.SchemaName);

    private static readonly WorkloadDescriptor s_webWorkload = new("Web", "Microsoft.VisualStudio.Component.Web");

    private readonly ConfiguredProject _project;
    private readonly ISetupComponentRegistrationService _setupComponentRegistrationService;
    private readonly IProjectSubscriptionService _projectSubscriptionService;
    private readonly IProjectFaultHandlerService _projectFaultHandlerService;
    private readonly DisposableBag _disposables = new();

    private Guid _projectGuid;
    private bool? _hasNoSuggestedWorkloads;
    private ISet<WorkloadDescriptor>? _suggestedWorkloads;

    [ImportingConstructor]
    public SetupComponentProvider(
        ConfiguredProject project,
        ISetupComponentRegistrationService setupComponentRegistrationService,
        IProjectSubscriptionService projectSubscriptionService,
        IProjectFaultHandlerService projectFaultHandlerService,
        IProjectThreadingService threadingService)
        : base(threadingService.JoinableTaskContext)
    {
        _project = project;
        _setupComponentRegistrationService = setupComponentRegistrationService;
        _projectSubscriptionService = projectSubscriptionService;
        _projectFaultHandlerService = projectFaultHandlerService;
    }

    public Task LoadAsync()
    {
        return InitializeAsync();
    }

    public Task UnloadAsync()
    {
        return Task.CompletedTask;
    }

    protected override Task DisposeCoreAsync(bool initialized)
    {
        _disposables.Dispose();

        return Task.CompletedTask;
    }

    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        // Note we don't use the ISafeProjectGuidService here because it is generally *not*
        // safe to use within IProjectDynamicLoadComponent.LoadAsync.
        _projectGuid = await _project.UnconfiguredProject.GetProjectGuidAsync();

        // Join the source blocks, so if they need to switch to UI thread to complete
        // and someone is blocked on us on the same thread, the call proceeds
        _disposables.Add(ProjectDataSources.JoinUpstreamDataSources(JoinableFactory, _projectFaultHandlerService, _projectSubscriptionService.ProjectRuleSource, _projectSubscriptionService.ProjectBuildRuleSource, _project.Capabilities));

        // Register this configured project with the aggregator.
        _disposables.Add(_setupComponentRegistrationService.RegisterProjectConfiguration(_projectGuid, _project));

        Action<IProjectVersionedValue<(IProjectSubscriptionUpdate EvaluationUpdate, IProjectSubscriptionUpdate BuildUpdate, IProjectCapabilitiesSnapshot Capabilities)>> action = OnUpdate;

        _disposables.Add(ProjectDataSources.SyncLinkTo(
            _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(s_evaluationRuleNames)),
            _projectSubscriptionService.ProjectBuildRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(s_buildRuleNames)),
            _project.Capabilities.SourceBlock.SyncLinkOptions(),
            target: DataflowBlockFactory.CreateActionBlock(action, _project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality),
            linkOptions: DataflowOption.PropagateCompletion,
            cancellationToken: cancellationToken));
    }

    private void OnUpdate(IProjectVersionedValue<(IProjectSubscriptionUpdate EvaluationUpdate, IProjectSubscriptionUpdate BuildUpdate, IProjectCapabilitiesSnapshot Capabilities)> update)
    {
        ProcessCapabilities(update.Value.Capabilities);
        ProcessEvaluationUpdate(update.Value.EvaluationUpdate);
        ProcessBuildUpdate(update.Value.BuildUpdate);

        void ProcessCapabilities(IProjectCapabilitiesSnapshot capabilities)
        {
            if (RequiresWebComponent())
            {
                // TODO cache allocation
                ImmutableHashSet<WorkloadDescriptor> workloads = ImmutableHashSet<WorkloadDescriptor>.Empty.Add(s_webWorkload);

                _setupComponentRegistrationService.SetSuggestedWebWorkloads(_projectGuid, _project, workloads);
            }

            bool RequiresWebComponent()
            {
                // Handle scenarios where Visual Studio developer may have an install of VS with only the desktop workload
                // and a developer may open a WPF/WinForms project (or edit an existing one) to be able to create a hybrid app (WPF + Blazor web).

                // DotNetCoreRazor && (WindowsForms || WPF)
                return capabilities.IsProjectCapabilityPresent(ProjectCapability.DotNetCoreRazor)
                    && (capabilities.IsProjectCapabilityPresent(ProjectCapability.WindowsForms) || capabilities.IsProjectCapabilityPresent(ProjectCapability.WPF));
            }
        }

        void ProcessEvaluationUpdate(IProjectSubscriptionUpdate update)
        {
            IImmutableDictionary<string, string> configurationGeneralProperties = update.CurrentState[ConfigurationGeneral.SchemaName].Properties;

            if (!configurationGeneralProperties.TryGetValue(ConfigurationGeneral.TargetFrameworkIdentifierProperty, out string? targetFrameworkIdentifier) ||
                !configurationGeneralProperties.TryGetValue(ConfigurationGeneral.TargetFrameworkVersionProperty, out string? targetFrameworkVersion) ||
                targetFrameworkVersion is null)
            {
                return;
            }

            // set to empty for non-netcore projects so that we do not check for missing installed runtime for them
            if (!string.Equals(targetFrameworkIdentifier, TargetFrameworkIdentifiers.NetCoreApp, StringComparisons.FrameworkIdentifiers) ||
                string.IsNullOrEmpty(targetFrameworkVersion))
            {
                targetFrameworkVersion = string.Empty;
            }

            _setupComponentRegistrationService.SetRuntimeVersion(_projectGuid, _project, targetFrameworkVersion);
        }

        void ProcessBuildUpdate(IProjectSubscriptionUpdate update)
        {
            // TODO no-op when data unchanged
            var workloads = CreateWorkloadDescriptor(update.CurrentState);

            ProcessWorkloads(workloads);

            void ProcessWorkloads(ISet<WorkloadDescriptor> workloads)
            {
                if (_hasNoSuggestedWorkloads != true)
                {
                    if (workloads.Count == 0)
                    {
                        _hasNoSuggestedWorkloads = true;
                    }
                    else
                    {
                        _suggestedWorkloads ??= new HashSet<WorkloadDescriptor>();
                        if (!_suggestedWorkloads.AddRange(workloads))
                        {
                            return;
                        }
                    }

                    _setupComponentRegistrationService.SetSuggestedWorkloads(_projectGuid, _project, workloads);
                }
            }

            static ISet<WorkloadDescriptor> CreateWorkloadDescriptor(IImmutableDictionary<string, IProjectRuleSnapshot> currentState)
            {
                IProjectRuleSnapshot suggestedWorkloads = currentState.GetSnapshotOrEmpty(SuggestedWorkload.SchemaName);

                if (suggestedWorkloads.Items.Count == 0)
                {
                    return ImmutableHashSet<WorkloadDescriptor>.Empty;
                }

                HashSet<WorkloadDescriptor>? workloads = null;

                if (suggestedWorkloads.Items.Count == 0)
                {
                    foreach ((string workloadName, IImmutableDictionary<string, string> metadata) in suggestedWorkloads.Items)
                    {
                        string? componentIds =
                            metadata.GetStringProperty(SuggestedWorkload.VisualStudioComponentIdsProperty) ??
                            metadata.GetStringProperty(SuggestedWorkload.VisualStudioComponentIdProperty);

                        if (componentIds is not null && !string.IsNullOrWhiteSpace(workloadName))
                        {
                            workloads ??= new();
                            workloads.Add(new WorkloadDescriptor(workloadName, componentIds));
                        }
                    }
                }

                return (ISet<WorkloadDescriptor>?)workloads ?? ImmutableHashSet<WorkloadDescriptor>.Empty;
            }
        }
    }
}
