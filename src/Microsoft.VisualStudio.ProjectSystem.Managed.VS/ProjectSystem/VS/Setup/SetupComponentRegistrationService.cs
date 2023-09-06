// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.RpcContracts.Setup;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

[Export(typeof(ISetupComponentRegistrationService))]
[Export(ExportContractNames.Scopes.ProjectService, typeof(IPackageService))]
internal sealed class SetupComponentRegistrationService : OnceInitializedOnceDisposedAsync, ISetupComponentRegistrationService, IVsSolutionEvents, IPackageService
{
    private const string WasmToolsWorkloadName = "wasm-tools";

    private static readonly ImmutableDictionary<string, string> s_runtimeVersionToComponentId = ImmutableStringDictionary<string>.EmptyOrdinalIgnoreCase
        .Add("v2.0", "Microsoft.Net.Core.Component.SDK.2.1")
        .Add("v2.1", "Microsoft.Net.Core.Component.SDK.2.1")
        .Add("v2.2", "Microsoft.Net.Core.Component.SDK.2.1")
        .Add("v3.0", "Microsoft.NetCore.Component.Runtime.3.1")
        .Add("v3.1", "Microsoft.NetCore.Component.Runtime.3.1")
        .Add("v5.0", "Microsoft.NetCore.Component.Runtime.5.0")
        .Add("v6.0", "Microsoft.NetCore.Component.Runtime.6.0")
        .Add("v7.0", "Microsoft.NetCore.Component.Runtime.7.0")
        .Add("v8.0", "Microsoft.NetCore.Component.Runtime.8.0");

    private static readonly ImmutableHashSet<string> s_supportedReleaseChannelWorkloads = ImmutableHashSet.Create(StringComparers.WorkloadNames, WasmToolsWorkloadName);

    // Services
    private readonly IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> _serviceBrokerContainer;
    private readonly IVsService<SVsSetupCompositionService, IVsSetupCompositionService> _vsSetupCompositionService;
    private readonly ISolutionService _solutionService;
    private readonly IProjectFaultHandlerService _projectFaultHandlerService;
    private readonly Lazy<HashSet<string>> _installedRuntimeVersions;
    private readonly Lazy<bool> _isPreviewChannel;

    // State
    private readonly ConcurrentHashSet<string> _webComponentIdsDetected = new(StringComparers.VisualStudioSetupComponentIds);
    private readonly ConcurrentDictionary<Guid, ConcurrentHashSet<WorkloadDescriptor>> _workloadsByProjectGuid = new();
    private readonly ConcurrentDictionary<Guid, string> _runtimeComponentIdByProjectGuid = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentHashSet<ProjectConfiguration>> _projectConfigurationsByProjectGuid = new();
    private ConcurrentDictionary<string, ConcurrentHashSet<ProjectConfiguration>>? _projectConfigurationsByProjectPath;
    private IAsyncDisposable? _solutionEventsSubscription;

    [ImportingConstructor]
    public SetupComponentRegistrationService(
        IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> serviceBrokerContainer,
        IVsService<SVsSetupCompositionService, IVsSetupCompositionService> vsSetupCompositionService,
        ISolutionService solutionService,
        Lazy<IVsShellUtilitiesHelper> vsShellUtilitiesHelper,
        IProjectFaultHandlerService projectFaultHandlerService,
        JoinableTaskContext joinableTaskContext)
        : base(new(joinableTaskContext))
    {
        _serviceBrokerContainer = serviceBrokerContainer;
        _vsSetupCompositionService = vsSetupCompositionService;
        _solutionService = solutionService;
        _projectFaultHandlerService = projectFaultHandlerService;

        // Workaround to fix https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1460328
        // VS has no information about the packages installed outside VS, and deep detection is not suggested for performance reasons.
        // This workaround reads the Registry Key HKLM\SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App
        // and get the installed runtime versions from the value names.
        _installedRuntimeVersions = new Lazy<HashSet<string>>(NetCoreRuntimeVersionsRegistryReader.ReadRuntimeVersionsInstalledInLocalMachine);

        _isPreviewChannel = new Lazy<bool>(() => vsShellUtilitiesHelper.Value.IsVSFromPreviewChannel());
    }

    Task IPackageService.InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
    {
        return InitializeAsync(CancellationToken.None);
    }

    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _solutionEventsSubscription = await _solutionService.SubscribeAsync(this, cancellationToken);
    }

    protected override async Task DisposeCoreAsync(bool initialized)
    {
        ClearData();

        if (_solutionEventsSubscription is not null)
        {
            await _solutionEventsSubscription.DisposeAsync();
        }
    }

    private void ClearData()
    {
        _webComponentIdsDetected.Clear();
        _runtimeComponentIdByProjectGuid.Clear();
        _workloadsByProjectGuid.Clear();
        _projectConfigurationsByProjectGuid.Clear();
        _projectConfigurationsByProjectPath?.Clear();
    }

    public void SetSuggestedWorkloads(Guid projectGuid, ConfiguredProject project, ISet<WorkloadDescriptor> workloadDescriptors)
    {
        if (workloadDescriptors.Count > 0)
        {
            ConcurrentHashSet<WorkloadDescriptor> workloadDescriptorSet = _workloadsByProjectGuid.GetOrAdd(projectGuid, guid => new());

            if (workloadDescriptorSet.AddRange(workloadDescriptors))
            {
                DisplayMissingComponentsPromptIfNeeded(project.UnconfiguredProject);
            }
        }

        UnregisterProjectConfiguration(projectGuid, project);
    }

    public void SetSuggestedWebWorkloads(Guid projectGuid, ConfiguredProject project, ISet<WorkloadDescriptor> workloadDescriptors)
    {
        if (AreNewComponentIdsToRegister(workloadDescriptors))
        {
            return;
        }

        ConcurrentHashSet<WorkloadDescriptor> workloadDescriptorSet = _workloadsByProjectGuid.GetOrAdd(projectGuid, static _ => new());

        workloadDescriptorSet.AddRange(workloadDescriptors);

        DisplayMissingComponentsPromptIfNeeded(project.UnconfiguredProject);

        bool AreNewComponentIdsToRegister(ISet<WorkloadDescriptor> workloadDescriptors)
        {
            bool added = false;

            foreach (WorkloadDescriptor workloadDescriptor in workloadDescriptors)
            {
                foreach (string componentId in workloadDescriptor.VisualStudioComponentIds)
                {
                    if (_webComponentIdsDetected.Add(componentId))
                    {
                        added = true;
                    }
                }
            }

            return added;
        }
    }

    public void SetRuntimeVersion(Guid projectGuid, ConfiguredProject project, string runtimeVersion)
    {
        // Check if the runtime is already installed in VS
        if (!string.IsNullOrEmpty(runtimeVersion) &&
            !_installedRuntimeVersions.Value.Contains(runtimeVersion) &&
            s_runtimeVersionToComponentId.TryGetValue(runtimeVersion, value: out string? componentId))
        {
            if (componentId is not null && _runtimeComponentIdByProjectGuid.TryAdd(projectGuid, componentId))
            {
                DisplayMissingComponentsPromptIfNeeded(project.UnconfiguredProject);
            }
        }

        UnregisterProjectConfiguration(projectGuid, project);
    }

    public IDisposable RegisterProjectConfiguration(Guid projectGuid, ConfiguredProject project)
    {
        AddConfiguration();

        return new DisposableDelegate(() => UnregisterProjectConfiguration(projectGuid, project));

        void AddConfiguration()
        {
            ConcurrentHashSet<ProjectConfiguration> projectConfigurations;

            // Fall back to the full path of the project if the project GUID has not yet been set.
            if (projectGuid == Guid.Empty)
            {
                // This collection is not commonly needed, so we construct it lazily.
                if (_projectConfigurationsByProjectPath is null)
                {
                    Interlocked.CompareExchange(ref _projectConfigurationsByProjectPath, new(StringComparers.Paths), null);
                }

                projectConfigurations = _projectConfigurationsByProjectPath.GetOrAdd(project.UnconfiguredProject.FullPath, static _ => new());
            }
            else
            {
                projectConfigurations = _projectConfigurationsByProjectGuid.GetOrAdd(projectGuid, static _ => new());
            }

            projectConfigurations.Add(project.ProjectConfiguration);
        }
    }

    private void UnregisterProjectConfiguration(Guid projectGuid, ConfiguredProject project)
    {
        RemoveConfiguration(projectGuid, project);

        void RemoveConfiguration(Guid projectGuid, ConfiguredProject project)
        {
            ConcurrentHashSet<ProjectConfiguration>? projectConfigurations = null;

            if (projectGuid == Guid.Empty)
            {
                _projectConfigurationsByProjectPath?.TryGetValue(project.UnconfiguredProject.FullPath, out projectConfigurations);
            }
            else
            {
                _projectConfigurationsByProjectGuid.TryGetValue(projectGuid, out projectConfigurations);
            }

            projectConfigurations?.Remove(project.ProjectConfiguration);
        }
    }

    private void DisplayMissingComponentsPromptIfNeeded(UnconfiguredProject project)
    {
        if (_workloadsByProjectGuid.IsEmpty && _runtimeComponentIdByProjectGuid.IsEmpty)
        {
            // No workloads or components were registered, so there's nothing that could need to be installed.
            return;
        }

        if (!AllProjectsConfigurationsRegisteredComponents())
        {
            // Still waiting on at least one registered project to provide its components.
            // We will wait until we have heard from every project.
            return;
        }

        Task task = DisplayMissingComponentsPromptAsync();

        _projectFaultHandlerService.Forget(task, project: project, ProjectFaultSeverity.Recoverable);

        return;

        bool AllProjectsConfigurationsRegisteredComponents()
        {
            // When a project configuration registers its required components, the configuration gets removed, but we keep the list of components.
            return _projectConfigurationsByProjectGuid.Values.All(configs => configs.Count == 0)
                && _projectConfigurationsByProjectPath?.Values.All(configs => configs.Count == 0) is null or true;
        }

        async Task DisplayMissingComponentsPromptAsync()
        {
            IVsSetupCompositionService? setupCompositionService = await _vsSetupCompositionService.GetValueAsync();

            if (setupCompositionService is null)
            {
                return;
            }

            IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? missingComponentIdsByProjectGuid = GetMissingComponentIdsByProjectGuid(setupCompositionService);

            if (missingComponentIdsByProjectGuid is null)
            {
                return;
            }

            IBrokeredServiceContainer serviceBrokerContainer = await _serviceBrokerContainer.GetValueAsync();
            IServiceBroker serviceBroker = serviceBrokerContainer.GetFullAccessServiceBroker();
            IMissingComponentRegistrationService? missingWorkloadRegistrationService = await serviceBroker.GetProxyAsync<IMissingComponentRegistrationService>(
                serviceDescriptor: VisualStudioServices.VS2022.MissingComponentRegistrationService);

            using (missingWorkloadRegistrationService as IDisposable)
            {
                if (missingWorkloadRegistrationService is not null)
                {
                    await missingWorkloadRegistrationService.RegisterMissingComponentsAsync(missingComponentIdsByProjectGuid, cancellationToken: default);
                }
            }

            return;

            IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? GetMissingComponentIdsByProjectGuid(IVsSetupCompositionService setupCompositionService)
            {
                // Values in this dictionary must be List<string> within this method.
                Dictionary<Guid, IReadOnlyCollection<string>> missingComponentIdsByProjectGuid = new();

                foreach ((Guid projectGuid, ConcurrentHashSet<WorkloadDescriptor> workloads) in _workloadsByProjectGuid)
                {
                    List<string> missingComponentIds = workloads
                        .Where(workload => IsSupportedWorkload(workload))
                        .SelectMany(workload => workload.VisualStudioComponentIds)
                        .Where(componentId => !setupCompositionService.IsPackageInstalled(componentId))
                        .ToList();

                    if (missingComponentIds.Count > 0)
                    {
                        missingComponentIdsByProjectGuid[projectGuid] = missingComponentIds;
                    }
                }

                // Add missing SDK runtime component IDs
                foreach ((Guid projectGuid, string runtimeComponentId) in _runtimeComponentIdByProjectGuid)
                {
                    if (setupCompositionService.IsPackageInstalled(runtimeComponentId))
                    {
                        continue;
                    }

                    if (missingComponentIdsByProjectGuid.TryGetValue(projectGuid, out IReadOnlyCollection<string>? missingComponentIds))
                    {
                        ((List<string>)missingComponentIds).Add(runtimeComponentId);
                    }
                    else
                    {
                        missingComponentIdsByProjectGuid.Add(projectGuid, new List<string>(capacity: 1) { runtimeComponentId });
                    }
                }

                if (missingComponentIdsByProjectGuid.Count == 0)
                {
                    return null;
                }

                return missingComponentIdsByProjectGuid;

                bool IsSupportedWorkload(WorkloadDescriptor workload)
                {
                    // TODO why are we blocking so many workloads here?
                    return s_supportedReleaseChannelWorkloads.Contains(workload.WorkloadName)
                        || _isPreviewChannel.Value;
                }
            }
        }
    }

    #region IVsSolutionEvents

    public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => HResult.NotImplemented;
    public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => HResult.NotImplemented;
    public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => HResult.NotImplemented;
    public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => HResult.NotImplemented;
    public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => HResult.NotImplemented;
    public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => HResult.NotImplemented;
    public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => HResult.NotImplemented;
    public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => HResult.NotImplemented;
    public int OnBeforeCloseSolution(object pUnkReserved) => HResult.NotImplemented;

    public int OnAfterCloseSolution(object pUnkReserved)
    {
        ClearData();

        return HResult.OK;
    }

    #endregion
}
