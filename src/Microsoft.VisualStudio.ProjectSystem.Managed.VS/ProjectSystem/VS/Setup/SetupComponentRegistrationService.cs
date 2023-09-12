// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
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
    private readonly Lazy<HashSet<string>?> _installedRuntimeComponentIds;

    private IVsSetupCompositionService? _setupCompositionService;
    private IMissingComponentRegistrationService? _missingComponentRegistrationService;
    private IAsyncDisposable? _solutionEventsSubscription;

    /// <summary>
    /// Stores the project's required setup components, keyed by project GUID.
    /// </summary>
    /// <remarks>
    /// When we call VS Setup (via <see cref="IMissingComponentRegistrationService"/>) we will pass data from all projects.
    /// To avoid churn, we don't make that call until we have data from all projects. To achieve this, we use a <see langword="null"/>
    /// value for projects that registered themselves (via <see cref="ISetupComponentRegistrationService.RegisterProjectAsync"/>)
    /// but have not yet provided their data (via <see cref="ISetupComponentRegistrationService.SetProjectComponentSnapshot"/>).
    /// In <see cref="TryPublish"/> we ensure that all values in this dictionary are non-<see langword="null"/> before publishing.
    /// </remarks>
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

        _installedRuntimeComponentIds = new Lazy<HashSet<string>?>(FindInstalledRuntimeComponentIds);

        static HashSet<string>? FindInstalledRuntimeComponentIds()
        {
            // Workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1460328
            // VS Setup doesn't know about runtimes installed outside of VS. Deep detection is not suggested for performance reasons.
            // Instead we read installed runtimes from the registry.
            //
            // We currently assume that the project will run on the same architecture as VS.
            // This will be the common case, but does not cover situations where a project runs
            // under emulation (e.g. an x64 build running on an ARM64 system).
            // TODO consider the architecture of the project itself
            Architecture architecture = RuntimeInformation.ProcessArchitecture;

            string[]? runtimeVersions = NetCoreRuntimeVersionsRegistryReader.ReadRuntimeVersionsInstalledInLocalMachine(architecture);

            if (runtimeVersions is null)
            {
                return null;
            }

            HashSet<string> installedRuntimeComponentIds = new(StringComparers.VisualStudioSetupComponentIds);

            foreach (string runtimeVersion in runtimeVersions)
            {
                // Example versions:
                //
                // - "3.1.32"
                // - "7.0.11"
                // - "8.0.0-preview.7.23375.6"
                // - "8.0.0-rc.1.23419.4"
                //
                // We only want the major/minor version, in the same format as TargetFrameworkVersion (e.g. "v8.0").

                int firstDotIndex = runtimeVersion.IndexOf('.');
                int secondDotIndex = runtimeVersion.IndexOf('.', firstDotIndex + 1);
                string versionNumber = runtimeVersion.Substring(0, secondDotIndex);
                string version = $"v{versionNumber}";

                if (SetupComponentReferenceData.TryGetComponentIdByNetCoreTargetFrameworkVersion(version, out string? runtimeComponentId))
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
            if (_setupCompositionService.IsPackageInstalled(componentId))
            {
                // Setup knows this component. It is not missing.
                return false;
            }

            if (_installedRuntimeComponentIds.Value is null)
            {
                // We couldn't determine installed runtimes, so err on the side of not reporting missing runtimes
                // rather than warning the user about something they do actually have installed.
                if (SetupComponentReferenceData.IsRuntimeComponentId(componentId))
                {
                    // This looks like a runtime component.
                    return false;
                }
            }
            else if (_installedRuntimeComponentIds.Value.Contains(componentId))
            {
                // We have a list of known installed runtimes and it includes this component.
                return false;
            }

            // This component is not known so is considered missing.
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
