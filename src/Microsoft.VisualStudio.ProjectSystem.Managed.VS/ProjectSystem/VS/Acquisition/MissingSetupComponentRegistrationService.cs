// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.Workloads;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Tracks the set of missing workload packs and SDK runtimes the .NET projects in a solution
    ///     need to improve the development experience.
    /// </summary>
    [Export(typeof(IMissingSetupComponentRegistrationService))]
    internal class MissingSetupComponentRegistrationService : IMissingSetupComponentRegistrationService, IVsSolutionEvents, IDisposable
    {
        private const string WasmToolsWorkloadName = "wasm-tools";

        private static readonly ImmutableHashSet<string> s_supportedReleaseChannelWorkloads = ImmutableHashSet.Create(StringComparers.WorkloadNames, WasmToolsWorkloadName);

        private readonly ConcurrentDictionary<Guid, IConcurrentHashSet<WorkloadDescriptor>> _projectGuidToWorkloadDescriptorsMap;
        private readonly ConcurrentDictionary<Guid, string> _projectGuidToRuntimeDescriptorMap;
        private readonly ConcurrentDictionary<Guid, IConcurrentHashSet<ProjectConfiguration>> _projectGuidToProjectConfigurationsMap;
        private readonly IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> _serviceBrokerContainer;
        private readonly IVsService<IVsSolution> _vsSolutionService;
        private readonly IVsService<SVsSetupCompositionService, IVsSetupCompositionService> _vsSetupCompositionService;
        private readonly Lazy<IVsShellUtilitiesHelper> _shellUtilitiesHelper;
        private readonly Lazy<IProjectThreadingService> _threadHandling;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;
        private readonly object _displayPromptLock = new();

        private ConcurrentDictionary<string, IConcurrentHashSet<ProjectConfiguration>>? _projectPathToProjectConfigurationsMap;
        private uint _solutionCookie = VSConstants.VSCOOKIE_NIL;
        private IVsSolution? _vsSolution;
        private bool? _isVSFromPreviewChannel;

        [ImportingConstructor]
        public MissingSetupComponentRegistrationService(
            IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> serviceBrokerContainer,
            IVsService<SVsSolution, IVsSolution> vsSolutionService,
            IVsService<SVsSetupCompositionService, IVsSetupCompositionService> vsSetupCompositionService,
            Lazy<IVsShellUtilitiesHelper> vsShellUtilitiesHelper,
            Lazy<IProjectThreadingService> threadHandling,
            IProjectFaultHandlerService projectFaultHandlerService)
        {
            _projectGuidToWorkloadDescriptorsMap = new();
            _projectGuidToProjectConfigurationsMap = new();
            _projectGuidToRuntimeDescriptorMap = new();

            _serviceBrokerContainer = serviceBrokerContainer;
            _vsSolutionService = vsSolutionService;
            _vsSetupCompositionService = vsSetupCompositionService;
            _threadHandling = threadHandling;
            _projectFaultHandlerService = projectFaultHandlerService;
            _shellUtilitiesHelper = vsShellUtilitiesHelper;
        }

        private ConcurrentDictionary<string, IConcurrentHashSet<ProjectConfiguration>> ProjectPathToProjectConfigurationsMap
        {
            get
            {
                if (_projectPathToProjectConfigurationsMap == null)
                {
                    Interlocked.CompareExchange(ref _projectPathToProjectConfigurationsMap, new(), null);
                }

                return _projectPathToProjectConfigurationsMap;
            }
        }

        private void ClearMissingWorkloadMetadata()
        {
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

        public void RegisterMissingSdkRuntimeComponentId(Guid projectGuid, ConfiguredProject project, string runtimeComponentId)
        {
            _projectGuidToRuntimeDescriptorMap.GetOrAdd(projectGuid, runtimeComponentId);

            UnregisterProjectConfiguration(projectGuid, project);
        }

        public void RegisterProjectConfiguration(Guid projectGuid, ConfiguredProject project)
        {
            if (project.ProjectConfiguration == null)
            {
                const string errorMessage = "Cannot register the project configuration for a null project configuration.";
                TraceUtilities.TraceError(errorMessage);

                System.Diagnostics.Debug.Fail(errorMessage);
                return;
            }

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

        public void UnregisterProjectConfiguration(Guid projectGuid, ConfiguredProject project)
        {
            IConcurrentHashSet<ProjectConfiguration> projectConfigurationSet;

            if (projectGuid == Guid.Empty)
            {
                if (ProjectPathToProjectConfigurationsMap.TryGetValue(project.UnconfiguredProject.FullPath, out projectConfigurationSet))
                {
                    projectConfigurationSet.Remove(project.ProjectConfiguration);
                }
            }
            else if (_projectGuidToProjectConfigurationsMap.TryGetValue(projectGuid, out projectConfigurationSet))
            {
                projectConfigurationSet.Remove(project.ProjectConfiguration);
            }

            bool displayMissingComponentsPrompt = ShouldDisplayMissingComponentsPrompt();
            if (displayMissingComponentsPrompt)
            {
                var displayMissingComponentsTask = DisplayMissingComponentsPromptAsync();

                _projectFaultHandlerService.Forget(displayMissingComponentsTask, project: null, ProjectFaultSeverity.LimitedFunctionality);
            }
        }

        private bool ShouldDisplayMissingComponentsPrompt()
        {
            lock (_displayPromptLock)
            {
                return (_projectGuidToWorkloadDescriptorsMap.Count > 0 || _projectGuidToRuntimeDescriptorMap.Count > 0)
                    && _projectGuidToProjectConfigurationsMap.Values.All(projectConfigurationSet => projectConfigurationSet?.Count == 0)
                    && (_projectPathToProjectConfigurationsMap == null ||
                        _projectPathToProjectConfigurationsMap.Values.All(projectConfigurationSet => projectConfigurationSet?.Count == 0));
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

            IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? vsComponentIdsToRegister = ComputeVsComponentIdsToRegister(setupCompositionService);
            if (vsComponentIdsToRegister == null)
            {
                return;
            }

            var serviceBrokerContainer = await _serviceBrokerContainer.GetValueAsync();
            IServiceBroker? serviceBroker = serviceBrokerContainer?.GetFullAccessServiceBroker();
            if (serviceBroker == null)
            {
                return;
            }

            var missingWorkloadRegistrationService = await serviceBroker.GetProxyAsync<RpcContracts.Setup.IMissingComponentRegistrationService>(
                serviceDescriptor: VisualStudioServices.VS2022.MissingComponentRegistrationService);

            using (missingWorkloadRegistrationService as IDisposable)
            {
                if (missingWorkloadRegistrationService != null)
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

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await _threadHandling.Value.SwitchToUIThread();

            _vsSolution = await _vsSolutionService.GetValueAsync();

            Verify.HResult(_vsSolution.AdviseSolutionEvents(this, out _solutionCookie));
        }

        public void Dispose()
        {
            _threadHandling.Value.VerifyOnUIThread();

            ClearMissingWorkloadMetadata();

            if (_solutionCookie != VSConstants.VSCOOKIE_NIL)
            {
                if (_vsSolution != null)
                {
                    Verify.HResult(_vsSolution.UnadviseSolutionEvents(_solutionCookie));
                    _solutionCookie = VSConstants.VSCOOKIE_NIL;
                    _vsSolution = null;
                }
            }
        }

        private bool IsSupportedWorkload(string workloadName)
        {
            return !string.IsNullOrWhiteSpace(workloadName)
                && (s_supportedReleaseChannelWorkloads.Contains(workloadName)
                    || _isVSFromPreviewChannel == true);
        }
    }
}
