// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Debugger.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

/// <summary>
/// An implementation of CPS's <see cref="IDebugLaunchProvider"/> that supports multiple launch profiles.
/// </summary>
/// <remarks>
/// Applies to projects having the <see cref="ProjectCapability.LaunchProfiles"/> capability.
/// </remarks>
[ExportDebugger(ProjectDebugger.SchemaName)]
[Export(typeof(IProjectHotReloadLaunchProvider))]
[AppliesTo(ProjectCapability.LaunchProfiles)]
internal class LaunchProfilesDebugLaunchProvider : DebugLaunchProviderBase, IDeployedProjectItemMappingProvider, IStartupProjectProvider, IProjectHotReloadLaunchProvider, IDebugLaunchProvider
{
    private readonly IVsService<IVsDebuggerLaunchAsync> _vsDebuggerService;
    // Launch providers to enforce requirements for debuggable projects
    private readonly ILaunchSettingsProvider _launchSettingsProvider;
    private IDebugProfileLaunchTargetsProvider? _lastLaunchProvider;
    private readonly IProjectThreadingService _threadingService;
    private readonly IProjectFaultHandlerService _projectFaultHandlerService;

    [ImportingConstructor]
    public LaunchProfilesDebugLaunchProvider(
        ConfiguredProject configuredProject,
        ILaunchSettingsProvider launchSettingsProvider,
        IVsService<IVsDebuggerLaunchAsync> vsDebuggerService,
        IProjectFaultHandlerService projectFaultHandlerService,
        [Import(AllowDefault = true)] IProjectThreadingService? threadingService = null)
        : base(configuredProject)
    {
        _launchSettingsProvider = launchSettingsProvider;
        _vsDebuggerService = vsDebuggerService;
        _projectFaultHandlerService = projectFaultHandlerService;
        _threadingService = threadingService ?? ThreadingService;

        LaunchTargetsProviders = new OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider>(projectCapabilityCheckProvider: configuredProject.UnconfiguredProject);
    }

    /// <summary>
    /// Import the LaunchTargetProviders which know how to run profiles
    /// </summary>
    [ImportMany]
    public OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider> LaunchTargetsProviders { get; }

    public override Task<bool> CanLaunchAsync(DebugLaunchOptions launchOptions) => TaskResult.True;

    public async Task<bool> CanBeStartupProjectAsync(DebugLaunchOptions launchOptions)
    {
        if (await GetActiveProfileAsync() is not ILaunchProfile activeProfile)
        {
            // If we can't identify the active launch profile, we can't start the project.
            return false;
        }

        // Find the DebugTargets provider for this profile
        IDebugProfileLaunchTargetsProvider? launchProvider = GetLaunchTargetsProvider(activeProfile);

        if (launchProvider is IDebugProfileLaunchTargetsProvider3 provider3)
        {
            return await provider3.CanBeStartupProjectAsync(launchOptions, activeProfile);
        }

        // Maintain backwards compat
        return true;
    }

    private async Task<ILaunchProfile?> GetActiveProfileAsync()
    {
        ILaunchSettings currentProfiles = await _launchSettingsProvider.WaitForFirstSnapshot();
        ILaunchProfile? activeProfile = currentProfiles.ActiveProfile;

        return activeProfile;
    }

    /// <summary>
    /// This is called to query the list of debug targets
    /// </summary>
    public override Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions)
    {
        return QueryDebugTargetsInternalAsync(launchOptions, fromDebugLaunch: false);
    }

    /// <summary>
    /// This is called on F5 to return the list of debug targets.
    /// What is returned depends on the debug provider extensions
    /// which understands how to launch the given profile type.
    /// If the given profile is null, the active profile will be used.
    /// </summary>
    private async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsInternalAsync(DebugLaunchOptions launchOptions, bool fromDebugLaunch, ILaunchProfile? profile = null)
    {
        profile ??= await GetActiveProfileAsync();

        if (profile is null)
        {
            throw new Exception(VSResources.ActiveLaunchProfileNotFound);
        }

        // Now find the DebugTargets provider for this profile
        IDebugProfileLaunchTargetsProvider launchProvider = GetLaunchTargetsProvider(profile) ??
            throw new Exception(string.Format(VSResources.DontKnowHowToRunProfile_2, profile.Name, profile.CommandName));

        IReadOnlyList<IDebugLaunchSettings> launchSettings;
        if (fromDebugLaunch && launchProvider is IDebugProfileLaunchTargetsProvider2 launchProvider2)
        {
            launchSettings = await launchProvider2.QueryDebugTargetsForDebugLaunchAsync(launchOptions, profile);
        }
        else
        {
            launchSettings = await launchProvider.QueryDebugTargetsAsync(launchOptions, profile);
        }

        _lastLaunchProvider = launchProvider;
        return launchSettings;
    }

    /// <summary>
    /// Returns the provider which knows how to launch the profile type.
    /// </summary>
    internal IDebugProfileLaunchTargetsProvider? GetLaunchTargetsProvider(ILaunchProfile profile)
    {
        // We search through the imports in order to find the one which supports the profile
        foreach (Lazy<IDebugProfileLaunchTargetsProvider, IOrderPrecedenceMetadataView> provider in LaunchTargetsProviders)
        {
            if (provider.Value.SupportsProfile(profile))
            {
                return provider.Value;
            }
        }

        return null;
    }

    private sealed class LaunchCompleteCallback(
        DebugLaunchOptions launchOptions,
        IDebugProfileLaunchTargetsProvider? targetsProvider,
        ILaunchProfile activeProfile,
        IReadOnlyList<IDebugLaunchSettings> debugLaunchSettings,
        IProjectFaultHandlerService projectFaultHandlerService,
        UnconfiguredProject? unconfiguredProject)
        : IVsDebuggerLaunchCompletionCallback, IVsDebugProcessNotify1800
    {
        private readonly List<IVsLaunchedProcess> _launchedProcesses = new();
        public void OnComplete(int hr, uint debugTargetCount, VsDebugTargetProcessInfo[] processInfoArray)
        {
            ErrorHandler.ThrowOnFailure(hr);

            if (targetsProvider is IDebugProfileLaunchTargetsProvider4 targetsProvider4)
            {
                IVsLaunchedProcess? vsLaunchedProcess = null;

                lock (_launchedProcesses)
                {
                    if (_launchedProcesses.Count == 1)
                    {
                        vsLaunchedProcess = _launchedProcesses[0];
                    }
                }

                if (targetsProvider4 is IDebugProfileLaunchTargetsProvider5 targetsProvider5 &&
                    debugLaunchSettings.Count == 1 &&
                    processInfoArray.Length == 1 &&
                    vsLaunchedProcess is not null)
                {
                    projectFaultHandlerService.Forget(
                        targetsProvider5.OnAfterLaunchAsync(launchOptions, activeProfile, debugLaunchSettings[0], vsLaunchedProcess, processInfoArray[0]),
                        unconfiguredProject);
                }
                else
                {
                    projectFaultHandlerService.Forget(
                        targetsProvider4.OnAfterLaunchAsync(launchOptions, activeProfile, processInfoArray),
                        unconfiguredProject);
                }
            }
            else if (targetsProvider is not null)
            {
                projectFaultHandlerService.Forget(
                    targetsProvider.OnAfterLaunchAsync(launchOptions, activeProfile),
                    unconfiguredProject);
            }
        }

        public int OnProcessLaunched(IVsLaunchedProcess pProcess)
        {
            lock (_launchedProcesses)
            {
                _launchedProcesses.Add(pProcess);
            }

            return HResult.OK;
        }
    }

    /// <summary>
    /// Overridden to direct the launch to the current active provider as determined by the active launch profile
    /// </summary>
    public override Task LaunchAsync(DebugLaunchOptions launchOptions)
    {
        ILaunchProfile? activeProfile = _launchSettingsProvider.ActiveProfile;

        Assumes.NotNull(activeProfile);

        return LaunchWithProfileAsync(launchOptions, activeProfile);
    }

    /// <summary>
    /// Launches the Visual Studio debugger using the specified profile.
    /// </summary>
    public async Task LaunchWithProfileAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IDebugLaunchSettings> targets = await QueryDebugTargetsInternalAsync(launchOptions, fromDebugLaunch: true, profile: profile);

        IDebugProfileLaunchTargetsProvider? targetProvider = GetLaunchTargetsProvider(profile);
        if (targetProvider is IDebugProfileLaunchTargetsProvider4 targetsProvider4)
        {
            await targetsProvider4.OnBeforeLaunchAsync(launchOptions, profile, targets);
        }
        else if (targetProvider is not null)
        {
            await targetProvider.OnBeforeLaunchAsync(launchOptions, profile);
        }
        
        LaunchCompleteCallback callback = new(launchOptions, targetProvider, profile, targets, _projectFaultHandlerService, ConfiguredProject?.UnconfiguredProject);

        if (targets.Count == 0)
        {
            callback.OnComplete(hr: 0, debugTargetCount: 0, processInfoArray: []);
            return;
        }

        VsDebugTargetInfo4[] launchSettingsNative = targets.Select(target =>
        {
            VsDebugTargetInfo4 nativeTarget = GetDebuggerStruct4(target);

            nativeTarget.pUnknown = callback;

            return nativeTarget;
        }).ToArray();

        try
        {
            IVsDebuggerLaunchAsync shellDebugger = await _vsDebuggerService.GetValueAsync();

            // The debugger needs to be called on the UI thread
            await _threadingService.SwitchToUIThread(cancellationToken);
            shellDebugger.LaunchDebugTargetsAsync((uint)launchSettingsNative.Length, launchSettingsNative, callback);
        }
        finally
        {
            // Free up the memory allocated to the (mostly) managed debugger structure.
            foreach (VsDebugTargetInfo4 nativeStruct in launchSettingsNative)
            {
                FreeDebuggerStruct(nativeStruct);
            }
        }
    }

    /// <summary>
    /// IDeployedProjectItemMappingProvider
    /// Implemented so that we can map URL's back to local file item paths
    /// </summary>
    public bool TryGetProjectItemPathFromDeployedPath(string deployedPath, out string? localPath)
    {
        // Just delegate to the last provider. It needs to figure out how best to map the items
        if (_lastLaunchProvider is IDeployedProjectItemMappingProvider deployedItemMapper)
        {
            return deployedItemMapper.TryGetProjectItemPathFromDeployedPath(deployedPath, out localPath);
        }

        // Return false to allow normal processing
        localPath = null;
        return false;
    }
}
