// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.RpcContracts.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

[Export(typeof(ISetupComponentRegistrationService))]
internal sealed class SetupComponentRegistrationService : OnceInitializedOnceDisposedAsync, ISetupComponentRegistrationService, IVsSolutionEvents
{
    private readonly IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> _serviceBrokerContainer;
    private readonly IVsService<SVsSetupCompositionService, IVsSetupCompositionService> _setupCompositionServiceFactory;
    private readonly ISolutionService _solutionService;
    private readonly IProjectFaultHandlerService _projectFaultHandlerService;
    private readonly Lazy<HashSet<string>> _installedRuntimeComponentIds;

    private IVsSetupCompositionService? _setupCompositionService;
    private IMissingComponentRegistrationService? _missingComponentRegistrationService;
    private IAsyncDisposable? _solutionEventsSubscription;
    private ImmutableDictionary<Guid, UnconfiguredSetupComponentSnapshot?> _snapshotByProjectGuid = ImmutableDictionary<Guid, UnconfiguredSetupComponentSnapshot?>.Empty;

    [ImportingConstructor]
    public SetupComponentRegistrationService(
        IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> serviceBrokerContainer,
        IVsService<SVsSetupCompositionService, IVsSetupCompositionService> vsSetupCompositionService,
        ISolutionService solutionService,
        IProjectFaultHandlerService projectFaultHandlerService,
        JoinableTaskContext joinableTaskContext)
        : base(new(joinableTaskContext))
    {
        _serviceBrokerContainer = serviceBrokerContainer;
        _setupCompositionServiceFactory = vsSetupCompositionService;
        _solutionService = solutionService;
        _projectFaultHandlerService = projectFaultHandlerService;

        // Workaround to fix https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1460328
        // VS has no information about the packages installed outside VS, and deep detection is not suggested for performance reasons.
        // This workaround reads the Registry Key HKLM\SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App
        // and get the installed runtime versions from the value names.
        _installedRuntimeComponentIds = new Lazy<HashSet<string>>(FindInstalledRuntimeComponentIds);

        static HashSet<string> FindInstalledRuntimeComponentIds()
        {
            HashSet<string> installedRuntimeComponentIds = new(StringComparers.VisualStudioSetupComponentIds);

            foreach (string runtimeVersion in NetCoreRuntimeVersionsRegistryReader.ReadRuntimeVersionsInstalledInLocalMachine())
            {
                if (ConfiguredSetupComponentSnapshot.ComponentIdByRuntimeVersion.TryGetValue(runtimeVersion, out string? runtimeComponentId))
                {
                    installedRuntimeComponentIds.Add(runtimeComponentId);
                }
            }

            return installedRuntimeComponentIds;
        }
    }

    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _solutionEventsSubscription = await _solutionService.SubscribeAsync(this, cancellationToken);

        _setupCompositionService = await _setupCompositionServiceFactory.GetValueAsync(cancellationToken);

        IBrokeredServiceContainer serviceBrokerContainer = await _serviceBrokerContainer.GetValueAsync(cancellationToken);
        IServiceBroker serviceBroker = serviceBrokerContainer.GetFullAccessServiceBroker();
        _missingComponentRegistrationService = await serviceBroker.GetProxyAsync<IMissingComponentRegistrationService>(
            serviceDescriptor: VisualStudioServices.VS2022.MissingComponentRegistrationService,
            cancellationToken: cancellationToken);
    }

    protected override async Task DisposeCoreAsync(bool initialized)
    {
        if (_solutionEventsSubscription is not null)
        {
            await _solutionEventsSubscription.DisposeAsync();
        }

        if (_missingComponentRegistrationService is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public async Task<IDisposable> RegisterProjectAsync(Guid projectGuid, CancellationToken cancellationToken)
    {
        Requires.Argument(projectGuid != Guid.Empty, nameof(projectGuid), "Cannot be an empty GUID.");

        await InitializeAsync(cancellationToken);

        // Add an empty snapshot for the project for now.
        ImmutableInterlocked.Update(
            ref _snapshotByProjectGuid,
            static (dic, projectGuid) => dic.Add(projectGuid, null),
            projectGuid);

        return new DisposableDelegate(() =>
        {
            ImmutableInterlocked.Update(
                ref _snapshotByProjectGuid,
                static (dic, projectGuid) => dic.Remove(projectGuid),
                projectGuid);
        });
    }

    public void SetProjectComponentSnapshot(Guid projectGuid, UnconfiguredSetupComponentSnapshot snapshot)
    {
        Requires.Argument(projectGuid != Guid.Empty, nameof(projectGuid), "Cannot be an empty GUID.");
        Requires.Argument(_snapshotByProjectGuid.ContainsKey(projectGuid), nameof(projectGuid), "Project GUID must be registered.");

        if (ImmutableInterlocked.Update(ref _snapshotByProjectGuid, static (dic, pair) => dic.SetItem(pair.Key, pair.Value), (Key: projectGuid, Value: snapshot)))
        {
            TryPublish();
        }
    }

    private void TryPublish()
    {
        // Grab a reference to this now so that we can work with a consistent view of the solution data without locking.
        ImmutableDictionary<Guid, UnconfiguredSetupComponentSnapshot?> snapshotByProjectGuid = _snapshotByProjectGuid;

        if (!HaveAllSnapshots())
        {
            return;
        }

        Assumes.Present(_setupCompositionService);
        Assumes.Present(_missingComponentRegistrationService);

        Dictionary<Guid, IReadOnlyCollection<string>>? missingComponentsByProjectGuid = null;

        foreach ((Guid projectGuid, UnconfiguredSetupComponentSnapshot? snapshot) in snapshotByProjectGuid)
        {
            Assumes.NotNull(snapshot);

            List<string>? missingComponents = null;

            foreach (string componentId in snapshot.ComponentIds)
            {
                if (IsMissingComponent(componentId))
                {
                    missingComponents ??= new();
                    missingComponents.Add(componentId);
                }
            }

            if (missingComponents is not null)
            {
                missingComponentsByProjectGuid ??= new();
                missingComponentsByProjectGuid.Add(projectGuid, missingComponents);
            }
        }

        if (missingComponentsByProjectGuid is not null)
        {
            Task task = _missingComponentRegistrationService.RegisterMissingComponentsAsync(missingComponentsByProjectGuid, cancellationToken: default);

            _projectFaultHandlerService.Forget(task, project: null, ProjectFaultSeverity.Recoverable);
        }

        return;

        bool HaveAllSnapshots()
        {
            foreach ((_, UnconfiguredSetupComponentSnapshot? snapshot) in snapshotByProjectGuid)
            {
                if (snapshot is null)
                {
                    return false;
                }
            }

            return true;
        }

        bool IsMissingComponent(string componentId)
        {
            if (_installedRuntimeComponentIds.Value.Contains(componentId))
            {
                return false;
            }

            if (_setupCompositionService.IsPackageInstalled(componentId))
            {
                return false;
            }

            return true;
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
        // Clear all data associated with the solution that's being closed
        _snapshotByProjectGuid = ImmutableDictionary<Guid, UnconfiguredSetupComponentSnapshot?>.Empty;

        return HResult.OK;
    }

    #endregion
}
