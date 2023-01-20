// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.Workloads;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Acquisition;

/// <summary>
///     Tracks the set of missing workload packs and SDK runtimes the .NET projects in a solution
///     need to improve the development experience.
/// </summary>
[Export(typeof(IMissingSetupComponentRegistrationService))]
internal class MissingSetupComponentRegistrationService : OnceInitializedOnceDisposedAsync, IMissingSetupComponentRegistrationService, IVsSolutionEvents
{
    private const string WasmToolsWorkloadName = "wasm-tools";

    private static readonly ImmutableDictionary<string, string> s_packageVersionToComponentId = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)
        .Add("v2.0", "Microsoft.Net.Core.Component.SDK.2.1")
        .Add("v2.1", "Microsoft.Net.Core.Component.SDK.2.1")
        .Add("v2.2", "Microsoft.Net.Core.Component.SDK.2.1")
        .Add("v3.0", "Microsoft.NetCore.Component.Runtime.3.1")
        .Add("v3.1", "Microsoft.NetCore.Component.Runtime.3.1")
        .Add("v5.0", "Microsoft.NetCore.Component.Runtime.5.0")
        .Add("v6.0", "Microsoft.NetCore.Component.Runtime.6.0")
        .Add("v7.0", "Microsoft.NetCore.Component.Runtime.7.0");

    private static readonly ImmutableHashSet<string> s_supportedReleaseChannelWorkloads = ImmutableHashSet.Create(StringComparers.WorkloadNames, WasmToolsWorkloadName);

    private readonly object _webComponentIdsDetectedLock = new();
    private readonly ConcurrentHashSet<string> _webComponentIdsDetected;
    private readonly ConcurrentHashSet<string> _missingRuntimesRegistered = new(StringComparers.WorkloadNames);
    private readonly ConcurrentDictionary<Guid, IConcurrentHashSet<WorkloadDescriptor>> _projectGuidToWorkloadDescriptorsMap;
    private readonly ConcurrentDictionary<Guid, string> _projectGuidToRuntimeDescriptorMap;
    private readonly ConcurrentDictionary<Guid, IConcurrentHashSet<ProjectConfiguration>> _projectGuidToProjectConfigurationsMap;
    private readonly IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> _serviceBrokerContainer;
    private readonly ISolutionService _solutionService;
    private readonly IVsService<SVsSetupCompositionService, IVsSetupCompositionService> _vsSetupCompositionService;
    private readonly Lazy<IVsShellUtilitiesHelper> _shellUtilitiesHelper;
    private readonly IProjectFaultHandlerService _projectFaultHandlerService;
    private readonly object _displayPromptLock = new();

    private ConcurrentDictionary<string, IConcurrentHashSet<ProjectConfiguration>>? _projectPathToProjectConfigurationsMap;
    private IAsyncDisposable? _solutionEventsSubscription;
    private bool? _isVSFromPreviewChannel;

    private readonly object _lock = new();
    private HashSet<string>? _netCoreRegistryKeyValues;

    [ImportingConstructor]
    public MissingSetupComponentRegistrationService(
        IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> serviceBrokerContainer,
        ISolutionService solutionService,
        IVsService<SVsSetupCompositionService, IVsSetupCompositionService> vsSetupCompositionService,
        Lazy<IVsShellUtilitiesHelper> vsShellUtilitiesHelper,
        IProjectFaultHandlerService projectFaultHandlerService,
        JoinableTaskContext joinableTaskContext)
        : base(new(joinableTaskContext))
    {
        _webComponentIdsDetected = new();
        _projectGuidToWorkloadDescriptorsMap = new();
        _projectGuidToProjectConfigurationsMap = new();
        _projectGuidToRuntimeDescriptorMap = new();

        _serviceBrokerContainer = serviceBrokerContainer;
        _solutionService = solutionService;
        _vsSetupCompositionService = vsSetupCompositionService;
        _projectFaultHandlerService = projectFaultHandlerService;
        _shellUtilitiesHelper = vsShellUtilitiesHelper;
    }

    private ConcurrentDictionary<string, IConcurrentHashSet<ProjectConfiguration>> ProjectPathToProjectConfigurationsMap
    {
        get
        {
            if (_projectPathToProjectConfigurationsMap is null)
            {
                Interlocked.CompareExchange(ref _projectPathToProjectConfigurationsMap, new(), null);
            }

            return _projectPathToProjectConfigurationsMap;
        }
    }

    private HashSet<string>? RuntimeVersionsInstalledInLocalMachine
    {
        get
        {
            if (_netCoreRegistryKeyValues is null)
            {
                lock (_lock)
                {
                    if (_netCoreRegistryKeyValues is null)
                    {
                        // Workaround to fix https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1460328
                        // VS has no information about the packages installed outside VS, and deep detection is not suggested for performance reasons.
                        // This workaround reads the Registry Key HKLM\SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App
                        // and get the installed runtime versions from the value names.
                        _netCoreRegistryKeyValues = NetCoreRuntimeVersionsRegistryReader.ReadRuntimeVersionsInstalledInLocalMachine();
                    }
                }
            }

            return _netCoreRegistryKeyValues;
        }
    }

    private void ClearMissingWorkloadMetadata()
    {
        _webComponentIdsDetected.Clear();
        _missingRuntimesRegistered.Clear();
        _projectGuidToRuntimeDescriptorMap.Clear();
        _projectGuidToWorkloadDescriptorsMap.Clear();
        _projectGuidToProjectConfigurationsMap.Clear();
        _projectPathToProjectConfigurationsMap?.Clear();
    }

    public void RegisterMissingWorkloads(Guid projectGuid, ConfiguredProject project, ISet<WorkloadDescriptor> workloadDescriptors)
    {
        if (workloadDescriptors.Count > 0)
        {
            var workloadDescriptorSet = _projectGuidToWorkloadDescriptorsMap.GetOrAdd(projectGuid, guid => new ConcurrentHashSet<WorkloadDescriptor>());
            workloadDescriptorSet.AddRange(workloadDescriptors);
        }

        UnregisterProjectConfiguration(projectGuid, project);
    }

    public void RegisterMissingWebWorkloads(Guid projectGuid, ConfiguredProject project, ISet<WorkloadDescriptor> workloadDescriptors)
    {
        if (AreNewComponentIdsToRegister(workloadDescriptors))
        {
            return;
        }

        var workloadDescriptorSet = _projectGuidToWorkloadDescriptorsMap.GetOrAdd(projectGuid, guid => new ConcurrentHashSet<WorkloadDescriptor>());
        workloadDescriptorSet.AddRange(workloadDescriptors);

        DisplayMissingComponentsPromptIfNeeded(project);

        bool AreNewComponentIdsToRegister(ISet<WorkloadDescriptor> workloadDescriptors)
        {
            bool notFound = false;
            foreach (var workloadDescriptor in workloadDescriptors)
            {
                foreach (var componentId in workloadDescriptor.VisualStudioComponentIds)
                {
                    lock (_webComponentIdsDetectedLock)
                    {
                        if (!_webComponentIdsDetected.Contains(componentId))
                        {
                            notFound = true;
                            _webComponentIdsDetected.Add(componentId);
                        }
                    }
                }
            }

            return !notFound;
        }
    }

    public void RegisterPossibleMissingSdkRuntimeVersion(Guid projectGuid, ConfiguredProject project, string runtimeVersion)
    {
        // Check if the runtime is already installed in VS
        if (!string.IsNullOrEmpty(runtimeVersion) &&
            (RuntimeVersionsInstalledInLocalMachine is null || !RuntimeVersionsInstalledInLocalMachine.Contains(runtimeVersion)) &&
            s_packageVersionToComponentId.TryGetValue(runtimeVersion, value: out string? componentId))
        {
            _projectGuidToRuntimeDescriptorMap.GetOrAdd(projectGuid, componentId);
        }

        UnregisterProjectConfiguration(projectGuid, project);
    }

    public void RegisterProjectConfiguration(Guid projectGuid, ConfiguredProject project)
    {
        if (project.ProjectConfiguration is null)
        {
            const string errorMessage = "Cannot register the project configuration for a null project configuration.";
            TraceUtilities.TraceError(errorMessage);

            System.Diagnostics.Debug.Fail(errorMessage);
            return;
        }

        AddConfiguration();

        void AddConfiguration()
        {
            IConcurrentHashSet<ProjectConfiguration> projectConfigurationSet;

            // Fall back to the full path of the project if the project GUID has not yet been set.
            if (projectGuid == Guid.Empty)
            {
                projectConfigurationSet = ProjectPathToProjectConfigurationsMap.GetOrAdd(project.UnconfiguredProject.FullPath, guid => new ConcurrentHashSet<ProjectConfiguration>());
            }
            else
            {
                projectConfigurationSet = _projectGuidToProjectConfigurationsMap.GetOrAdd(projectGuid, guid => new ConcurrentHashSet<ProjectConfiguration>());
            }

            projectConfigurationSet.Add(project.ProjectConfiguration);
        }
    }

    public void UnregisterProjectConfiguration(Guid projectGuid, ConfiguredProject project)
    {
        RemoveConfiguration(projectGuid, project);
        DisplayMissingComponentsPromptIfNeeded(project);

        void RemoveConfiguration(Guid projectGuid, ConfiguredProject project)
        {
            IConcurrentHashSet<ProjectConfiguration>? projectConfigurationSet;

            if (projectGuid == Guid.Empty)
            {
                ProjectPathToProjectConfigurationsMap.TryGetValue(project.UnconfiguredProject.FullPath, out projectConfigurationSet);
            }
            else
            {
                _projectGuidToProjectConfigurationsMap.TryGetValue(projectGuid, out projectConfigurationSet);
            }

            projectConfigurationSet?.Remove(project.ProjectConfiguration);
        }
    }

    private void DisplayMissingComponentsPromptIfNeeded(ConfiguredProject project)
    {
        bool displayMissingComponentsPrompt = ShouldDisplayMissingComponentsPrompt();
        if (displayMissingComponentsPrompt)
        {
            var displayMissingComponentsTask = DisplayMissingComponentsPromptAsync();

            _projectFaultHandlerService.Forget(displayMissingComponentsTask, project: project.UnconfiguredProject, ProjectFaultSeverity.Recoverable);
        }
    }

    private bool ShouldDisplayMissingComponentsPrompt()
    {
        lock (_displayPromptLock)
        {
            // Projects that subscribe to this service will registers all their configurations and after that
            // each project configuration can start registering missing workload at different point in time.
            // We want to display the prompt after ALL the registered project already registered their missing components
            // and at least there is one component to install.
            return AreMissingComponentsToInstall()
                && AllProjectsConfigurationsRegisteredTheirMissingComponents();
        }

        bool AreMissingComponentsToInstall()
        {
            // Projects can register zero or more missing components.
            return _projectGuidToWorkloadDescriptorsMap.Count > 0 || _projectGuidToRuntimeDescriptorMap.Count > 0;
        }

        bool AllProjectsConfigurationsRegisteredTheirMissingComponents()
        {
            // When a project configuration registers its missing components, the configuration gets removed, but we keep the list of components.
            return _projectGuidToProjectConfigurationsMap.Values.All(projectConfigurationSet => projectConfigurationSet.Count == 0)
                && _projectPathToProjectConfigurationsMap?.Values.All(projectConfigurationSet => projectConfigurationSet.Count == 0) is null or true;
        }
    }

    private async Task DisplayMissingComponentsPromptAsync()
    {
        if (!_isVSFromPreviewChannel.HasValue)
        {
            _isVSFromPreviewChannel = await _shellUtilitiesHelper.Value.IsVSFromPreviewChannelAsync();
            await TaskScheduler.Default;
        }

        var setupCompositionService = await _vsSetupCompositionService.GetValueAsync();
        if (setupCompositionService is null)
        {
            return;
        }

        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? vsComponentIdsToRegister = ComputeVsComponentIdsToRegister(setupCompositionService);
        if (vsComponentIdsToRegister is null)
        {
            return;
        }

        var serviceBrokerContainer = await _serviceBrokerContainer.GetValueAsync();
        IServiceBroker? serviceBroker = serviceBrokerContainer?.GetFullAccessServiceBroker();
        if (serviceBroker is null)
        {
            return;
        }

        var missingWorkloadRegistrationService = await serviceBroker.GetProxyAsync<RpcContracts.Setup.IMissingComponentRegistrationService>(
            serviceDescriptor: VisualStudioServices.VS2022.MissingComponentRegistrationService);

        using (missingWorkloadRegistrationService as IDisposable)
        {
            if (missingWorkloadRegistrationService is not null)
            {
                await missingWorkloadRegistrationService.RegisterMissingComponentsAsync(vsComponentIdsToRegister, cancellationToken: default);
            }
        }
    }

    private IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? ComputeVsComponentIdsToRegister(IVsSetupCompositionService setupCompositionService)
    {
        if (_projectGuidToWorkloadDescriptorsMap.Count == 0 && _projectGuidToRuntimeDescriptorMap.Count == 0)
        {
            return null;
        }

        Dictionary<Guid, IReadOnlyCollection<string>> vsComponentIdsToRegister = new();

        foreach (var (projectGuid, vsComponents) in _projectGuidToWorkloadDescriptorsMap)
        {
            var vsComponentIds = vsComponents.Where(descriptor => IsSupportedWorkload(descriptor.WorkloadName))
                                             .SelectMany(workloadDescriptor => workloadDescriptor.VisualStudioComponentIds)
                                             .Where(vsComponentId => !setupCompositionService.IsPackageInstalled(vsComponentId))
                                             .ToArray();

            if (vsComponentIds.Length > 0)
            {
                vsComponentIdsToRegister[projectGuid] = vsComponentIds;
            }
        }

        AddMissingSdkRuntimeComponentIds(setupCompositionService, vsComponentIdsToRegister);

        if (vsComponentIdsToRegister.Count == 0)
        {
            return null;
        }

        return vsComponentIdsToRegister;
    }

    private bool IsSupportedWorkload(string workloadName)
    {
        return !string.IsNullOrWhiteSpace(workloadName)
            && (s_supportedReleaseChannelWorkloads.Contains(workloadName)
                || _isVSFromPreviewChannel == true);
    }

    private void AddMissingSdkRuntimeComponentIds(IVsSetupCompositionService setupCompositionService, Dictionary<Guid, IReadOnlyCollection<string>> vsComponentIdsToRegister)
    {
        foreach (var (projectGuid, runtimeComponentId) in _projectGuidToRuntimeDescriptorMap)
        {
            if (setupCompositionService.IsPackageInstalled(runtimeComponentId))
            {
                continue;
            }

            vsComponentIdsToRegister.TryGetValue(projectGuid, out IReadOnlyCollection<string>? workloadVsComponent);

            IEnumerable<string> runtimeVsComponents = workloadVsComponent is not null ?
                 workloadVsComponent.Append(runtimeComponentId)
                 : new List<string>() { runtimeComponentId };

            vsComponentIdsToRegister[projectGuid] = runtimeVsComponents.ToImmutableList();
        }
    }

    #region IVsSolutionEvents

    public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
    {
        return HResult.NotImplemented;
    }

    public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
    {
        return HResult.NotImplemented;
    }

    public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
    {
        return HResult.NotImplemented;
    }

    public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
    {
        return HResult.NotImplemented;
    }

    public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
    {
        return HResult.NotImplemented;
    }

    public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
    {
        return HResult.NotImplemented;
    }

    public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
    {
        return HResult.NotImplemented;
    }

    public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
    {
        return HResult.NotImplemented;
    }

    public int OnBeforeCloseSolution(object pUnkReserved)
    {
        return HResult.NotImplemented;
    }

    public int OnAfterCloseSolution(object pUnkReserved)
    {
        ClearMissingWorkloadMetadata();

        return HResult.OK;
    }

    #endregion

    Task IMissingSetupComponentRegistrationService.InitializeAsync(CancellationToken cancellationToken)
    {
        return InitializeAsync(cancellationToken);
    }

    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _solutionEventsSubscription = await _solutionService.SubscribeAsync(this, cancellationToken);
    }

    protected override async Task DisposeCoreAsync(bool initialized)
    {
        ClearMissingWorkloadMetadata();

        if (_solutionEventsSubscription is not null)
        {
            await _solutionEventsSubscription.DisposeAsync();
        }
    }
}
