// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Determines the VS setup component requirements of the configured project and provides them to <see cref="IMissingSetupComponentRegistrationService"/>/>.
///     Tracks the set of missing .NET workloads for a configured project.
///     
/// Detect during solution load the Net Core runtime version based on the target framework 
/// used in a project file and pass this version to a service that can install the
/// runtime component if not installed.
/// </summary>
[Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
[AppliesTo(ProjectCapability.DotNet)]
internal sealed class MissingComponentProvider : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
{
    private static readonly IImmutableSet<string> s_evaluationRuleNames = ImmutableHashSet<string>.Empty.WithComparer(StringComparers.RuleNames).Add(ConfigurationGeneral.SchemaName);
    private static readonly IImmutableSet<string> s_buildRuleNames = ImmutableHashSet<string>.Empty.WithComparer(StringComparers.RuleNames).Add(SuggestedWorkload.SchemaName);

    private static readonly WorkloadDescriptor s_webWorkload = new("Web", "Microsoft.VisualStudio.Component.Web");

    private readonly ConfiguredProject _project;
    private readonly IMissingSetupComponentRegistrationService _missingSetupComponentRegistrationService;
    private readonly IProjectSubscriptionService _projectSubscriptionService;
    private readonly IProjectFaultHandlerService _projectFaultHandlerService;
    private readonly DisposableBag _disposables = new();

    private Guid _projectGuid;
    private bool? _hasNoMissingWorkloads;
    private ISet<WorkloadDescriptor>? _missingWorkloads;

    [ImportingConstructor]
    public MissingComponentProvider(
        ConfiguredProject project,
        IMissingSetupComponentRegistrationService missingSetupComponentRegistrationService,
        IProjectSubscriptionService projectSubscriptionService,
        IProjectFaultHandlerService projectFaultHandlerService,
        IProjectThreadingService threadingService)
        : base(threadingService.JoinableTaskContext)
    {
        _project = project;
        _missingSetupComponentRegistrationService = missingSetupComponentRegistrationService;
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
        _disposables.Add(_missingSetupComponentRegistrationService.RegisterProjectConfiguration(_projectGuid, _project));

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

                _missingSetupComponentRegistrationService.RegisterMissingWebWorkloads(_projectGuid, _project, workloads);
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

            _missingSetupComponentRegistrationService.RegisterPossibleMissingSdkRuntimeVersion(_projectGuid, _project, targetFrameworkVersion);
        }

        void ProcessBuildUpdate(IProjectSubscriptionUpdate update)
        {
            // TODO no-op when data unchanged
            var workloads = CreateWorkloadDescriptor(update.CurrentState);

            ProcessWorkloads(workloads);

            void ProcessWorkloads(ISet<WorkloadDescriptor> workloads)
            {
                if (_hasNoMissingWorkloads != true)
                {
                    if (workloads.Count == 0)
                    {
                        _hasNoMissingWorkloads = true;
                    }
                    else
                    {
                        _missingWorkloads ??= new HashSet<WorkloadDescriptor>();
                        if (!_missingWorkloads.AddRange(workloads))
                        {
                            return;
                        }
                    }

                    _missingSetupComponentRegistrationService.RegisterMissingWorkloads(_projectGuid, _project, workloads);
                }
            }

            static ISet<WorkloadDescriptor> CreateWorkloadDescriptor(IImmutableDictionary<string, IProjectRuleSnapshot> currentState)
            {
                IProjectRuleSnapshot suggestedWorkloads = currentState.GetSnapshotOrEmpty(SuggestedWorkload.SchemaName);

                if (suggestedWorkloads.Items.Count == 0)
                {
                    return ImmutableHashSet<WorkloadDescriptor>.Empty;
                }

                var workloadDescriptors = suggestedWorkloads.Items.Select(item =>
                {
                    string workloadName = item.Key;

                    if (!string.IsNullOrWhiteSpace(workloadName)
                        && (item.Value.TryGetStringProperty(SuggestedWorkload.VisualStudioComponentIdsProperty, out string? vsComponentIds)
                         || item.Value.TryGetStringProperty(SuggestedWorkload.VisualStudioComponentIdProperty, out vsComponentIds)))
                    {
                        return new WorkloadDescriptor(workloadName, vsComponentIds);
                    }

                    return WorkloadDescriptor.Empty;
                });

                return new HashSet<WorkloadDescriptor>(workloadDescriptors);
            }
        }
    }
}
