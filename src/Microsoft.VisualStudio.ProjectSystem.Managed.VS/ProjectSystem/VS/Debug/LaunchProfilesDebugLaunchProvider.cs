// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// The exported CPS debugger for all types of K projects (web, consoles, class libraries). Defers to
    /// other types to get the DebugTarget information to launch.
    /// </summary>
    [ExportDebugger(ProjectDebugger.SchemaName)]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class LaunchProfilesDebugLaunchProvider : DebugLaunchProviderBase, IDeployedProjectItemMappingProvider, IStartupProjectProvider
    {
        private readonly IVsService<IVsDebuggerLaunchAsync> _vsDebuggerService;
        // Launch providers to enforce requirements for debuggable projects
        private readonly ILaunchSettingsProvider _launchSettingsProvider;
        private IDebugProfileLaunchTargetsProvider? _lastLaunchProvider;

        [ImportingConstructor]
        public LaunchProfilesDebugLaunchProvider(
            ConfiguredProject configuredProject,
            ILaunchSettingsProvider launchSettingsProvider,
            IVsService<IVsDebuggerLaunchAsync> vsDebuggerService)
            : base(configuredProject)
        {
            _launchSettingsProvider = launchSettingsProvider;
            _vsDebuggerService = vsDebuggerService;

            LaunchTargetsProviders = new OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider>(projectCapabilityCheckProvider: configuredProject.UnconfiguredProject);
        }

        /// <summary>
        /// Import the LaunchTargetProviders which know how to run profiles
        /// </summary>
        [ImportMany]
        public OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider> LaunchTargetsProviders { get; }

        /// <summary>
        /// Called by CPS to determine whether we can launch
        /// </summary>
        public override Task<bool> CanLaunchAsync(DebugLaunchOptions launchOptions) => TplExtensions.TrueTask;

        /// <summary>
        /// Called by StartupProjectRegistrar to determine whether this project should appear in the Startup list.
        /// </summary>
        public async Task<bool> CanBeStartupProjectAsync(DebugLaunchOptions launchOptions)
        {
            if (await GetActiveProfileAsync() is ILaunchProfile activeProfile)
            {
                // Now find the DebugTargets provider for this profile
                IDebugProfileLaunchTargetsProvider? launchProvider = GetLaunchTargetsProvider(activeProfile);

                if (launchProvider is IDebugProfileLaunchTargetsProvider3 provider3)
                {
                    return await provider3.CanBeStartupProjectAsync(launchOptions, activeProfile);
                }

                // Maintain backwards compat
                return true;
            }

            // If we can't identify the active launch profile, we can't start the project.
            return false;
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
        /// This is called on F5 to return the list of debug targets. What is returned depends on the debug provider extensions
        /// which understands how to launch the currently active profile type.
        /// </summary>
        private async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsInternalAsync(DebugLaunchOptions launchOptions, bool fromDebugLaunch)
        {
            ILaunchProfile? activeProfile = await GetActiveProfileAsync();

            if (activeProfile is null)
            {
                throw new Exception(VSResources.ActiveLaunchProfileNotFound);
            }

            // Now find the DebugTargets provider for this profile
            IDebugProfileLaunchTargetsProvider launchProvider = GetLaunchTargetsProvider(activeProfile) ??
                throw new Exception(string.Format(VSResources.DontKnowHowToRunProfile_2, activeProfile.Name, activeProfile.CommandName));

            IReadOnlyList<IDebugLaunchSettings> launchSettings;
            if (fromDebugLaunch && launchProvider is IDebugProfileLaunchTargetsProvider2 launchProvider2)
            {
                launchSettings = await launchProvider2.QueryDebugTargetsForDebugLaunchAsync(launchOptions, activeProfile);
            }
            else
            {
                launchSettings = await launchProvider.QueryDebugTargetsAsync(launchOptions, activeProfile);
            }

            _lastLaunchProvider = launchProvider;
            return launchSettings;
        }

        /// <summary>
        /// Returns the provider which knows how to launch the profile type.
        /// </summary>
        public IDebugProfileLaunchTargetsProvider? GetLaunchTargetsProvider(ILaunchProfile profile)
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

        private class LaunchCompleteCallback : IVsDebuggerLaunchCompletionCallback
        {
            private readonly DebugLaunchOptions _launchOptions;
            private readonly IDebugProfileLaunchTargetsProvider? _targetsProvider;
            private readonly ILaunchProfile _activeProfile;
            private readonly IProjectThreadingService _threadingService;

            public LaunchCompleteCallback(IProjectThreadingService threadingService, DebugLaunchOptions launchOptions, IDebugProfileLaunchTargetsProvider? targetsProvider, ILaunchProfile activeProfile)
            {
                _threadingService = threadingService;
                _launchOptions = launchOptions;
                _targetsProvider = targetsProvider;
                _activeProfile = activeProfile;
            }

            public void OnComplete(int hr, uint debugTargetCount, VsDebugTargetProcessInfo[] processInfoArray)
            {
                if (_targetsProvider is IDebugProfileLaunchTargetsProvider4 targetsProvider4)
                {
                    _threadingService.ExecuteSynchronously(() => targetsProvider4.OnAfterLaunchAsync(_launchOptions, _activeProfile, processInfoArray));
                }
                else if (_targetsProvider is not null)
                {
                    _threadingService.ExecuteSynchronously(() => _targetsProvider.OnAfterLaunchAsync(_launchOptions, _activeProfile));
                }
            }
        };

        /// <summary>
        /// Overridden to direct the launch to the current active provider as determined by the active launch profile
        /// </summary>
        public override async Task LaunchAsync(DebugLaunchOptions launchOptions)
        {
            IReadOnlyList<IDebugLaunchSettings> targets = await QueryDebugTargetsInternalAsync(launchOptions, fromDebugLaunch: true);

            ILaunchProfile? activeProfile = _launchSettingsProvider.ActiveProfile;

            Assumes.NotNull(activeProfile);

            IDebugProfileLaunchTargetsProvider? targetProvider = GetLaunchTargetsProvider(activeProfile);
            if (targetProvider is IDebugProfileLaunchTargetsProvider4 targetsProvider4)
            {
                await targetsProvider4.OnBeforeLaunchAsync(launchOptions, activeProfile, targets);
            }
            else if (targetProvider is not null)
            {
                await targetProvider.OnBeforeLaunchAsync(launchOptions, activeProfile);
            }

            await DoLaunchAsync(new LaunchCompleteCallback(ThreadingService, launchOptions, targetProvider, activeProfile), targets.ToArray());
        }

        /// <summary>
        /// Launches the Visual Studio debugger.
        /// </summary>
        protected async Task DoLaunchAsync(IVsDebuggerLaunchCompletionCallback cb, params IDebugLaunchSettings[] launchSettings)
        {
            if (launchSettings.Length == 0)
            {
                cb.OnComplete(0, 0, null);
                return;
            }

            VsDebugTargetInfo4[] launchSettingsNative = launchSettings.Select(GetDebuggerStruct4).ToArray();

            try
            {
                // The debugger needs to be called on the UI thread
                await ThreadingService.SwitchToUIThread();

                IVsDebuggerLaunchAsync shellDebugger = await _vsDebuggerService.GetValueAsync();
                shellDebugger.LaunchDebugTargetsAsync((uint)launchSettingsNative.Length, launchSettingsNative, cb);
            }
            finally
            {
                // Free up the memory allocated to the (mostly) managed debugger structure.
                foreach (VsDebugTargetInfo4 nativeStruct in launchSettingsNative)
                {
                    FreeVsDebugTargetInfoStruct(nativeStruct);
                }
            }
        }

        /// <summary>
        /// Copy information over from the contract struct to the native one.
        /// </summary>
        /// <returns>The native struct.</returns>
        internal static VsDebugTargetInfo4 GetDebuggerStruct4(IDebugLaunchSettings info)
        {
            var debugInfo = new VsDebugTargetInfo4
            {
                // **Begin common section -- keep this in sync with GetDebuggerStruct**
                dlo = (uint)info.LaunchOperation,
                LaunchFlags = (uint)info.LaunchOptions,
                bstrRemoteMachine = info.RemoteMachine,
                bstrArg = info.Arguments,
                bstrCurDir = info.CurrentDirectory,
                bstrExe = info.Executable,

                bstrEnv = GetSerializedEnvironmentString(info.Environment),
                guidLaunchDebugEngine = info.LaunchDebugEngineGuid
            };

            var guids = new List<Guid>(1 + info.AdditionalDebugEngines?.Count ?? 0)
            {
                info.LaunchDebugEngineGuid
            };

            if (info.AdditionalDebugEngines is not null)
            {
                guids.AddRange(info.AdditionalDebugEngines);
            }

            debugInfo.dwDebugEngineCount = (uint)guids.Count;

            byte[] guidBytes = GetGuidBytes(guids);
            debugInfo.pDebugEngines = Marshal.AllocCoTaskMem(guidBytes.Length);
            Marshal.Copy(guidBytes, 0, debugInfo.pDebugEngines, guidBytes.Length);

            debugInfo.guidPortSupplier = info.PortSupplierGuid;
            debugInfo.bstrPortName = info.PortName;
            debugInfo.bstrOptions = info.Options;
            debugInfo.fSendToOutputWindow = info.SendToOutputWindow ? true : false;
            debugInfo.dwProcessId = unchecked((uint)info.ProcessId);
            debugInfo.pUnknown = info.Unknown;
            debugInfo.guidProcessLanguage = info.ProcessLanguageGuid;

            // **End common section**

            if (info.StandardErrorHandle != IntPtr.Zero || info.StandardInputHandle != IntPtr.Zero || info.StandardOutputHandle != IntPtr.Zero)
            {
                var processStartupInfo = new VsDebugStartupInfo
                {
                    hStdInput = unchecked(info.StandardInputHandle),
                    hStdOutput = unchecked(info.StandardOutputHandle),
                    hStdError = unchecked(info.StandardErrorHandle),
                    flags = (uint)__DSI_FLAGS.DSI_USESTDHANDLES,
                };
                debugInfo.pStartupInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(processStartupInfo));
                Marshal.StructureToPtr(processStartupInfo, debugInfo.pStartupInfo, false);
            }

            debugInfo.AppPackageLaunchInfo = info.AppPackageLaunchInfo;
            debugInfo.project = info.Project;

            return debugInfo;
        }

        /// <summary>
        /// Frees memory allocated by GetDebuggerStruct.
        /// </summary>
        internal static void FreeVsDebugTargetInfoStruct(VsDebugTargetInfo4 nativeStruct)
        {
            if (nativeStruct.pDebugEngines != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(nativeStruct.pDebugEngines);
            }

            if (nativeStruct.pStartupInfo != IntPtr.Zero)
            {
                Marshal.DestroyStructure(nativeStruct.pStartupInfo, typeof(VsDebugStartupInfo));
                Marshal.FreeCoTaskMem(nativeStruct.pStartupInfo);
            }
        }

        /// <summary>
        /// Converts the environment key value pairs to a valid environment string of the form
        /// key=value/0key2=value2/0/0, with nulls between each entry and a double null terminator.
        /// </summary>
        private static string? GetSerializedEnvironmentString(IDictionary<string, string> environment)
        {
            // If no dictionary was set, or its empty, the debugger wants null for its environment block.
            if (environment is null || environment.Count == 0)
            {
                return null;
            }

            // Collect all the variables as a null delimited list of key=value pairs.
            var result = PooledStringBuilder.GetInstance();
            foreach ((string key, string value) in environment)
            {
                result.Append(key);
                result.Append('=');
                result.Append(value);
                result.Append('\0');
            }

            // Add a final list-terminating null character.
            // This is sent to native code as a BSTR and no null is added automatically.
            // But the contract of the format of the data requires that this be a null-delimited,
            // null-terminated list.
            result.Append('\0');
            return result.ToStringAndFree();
        }

        /// <summary>
        /// Collects an array of GUIDs into an array of bytes.
        /// </summary>
        /// <remarks>
        /// The order of the GUIDs are preserved, and each GUID is copied exactly one after the other in the byte array.
        /// </remarks>
        private static byte[] GetGuidBytes(IList<Guid> guids)
        {
            const int sizeOfGuid = 16;
            byte[] bytes = new byte[guids.Count * sizeOfGuid];
            for (int i = 0; i < guids.Count; i++)
            {
                byte[] guidBytes = guids[i].ToByteArray();
                guidBytes.CopyTo(bytes, i * sizeOfGuid);
            }

            return bytes;
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
}
