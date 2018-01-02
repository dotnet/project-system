// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{

    /// <summary>
    /// The exported CPS debugger for all types of K projects (web, consoles, classlibraries). Defers to
    /// other types to get the DebugTarget information to launch.
    /// </summary>
    [ExportDebugger(ProjectDebugger.SchemaName)]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class ProjectDebuggerProvider : DebugLaunchProviderBase, IDeployedProjectItemMappingProvider
    {

        /// <summary>
        /// Constructors. Unit test one is 2nd
        /// </summary>
        [ImportingConstructor]
        public ProjectDebuggerProvider(ConfiguredProject configuredProject, ILaunchSettingsProvider launchSettingsProvider)
            : base(configuredProject)
        {
            LaunchSettingsProvider = launchSettingsProvider;

            // We want it sorted so that higher numbers come first (is the default for these collections but explicitly expressed here)
            ProfileLaunchTargetsProviders = new OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst,
                                                                                                                    configuredProject.UnconfiguredProject);
        }

        public ProjectDebuggerProvider(ConfiguredProject configuredProject, ILaunchSettingsProvider launchSettingsProvider,
                                       OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider> providers)
            : base(configuredProject)
        {
            ProfileLaunchTargetsProviders = providers;
            LaunchSettingsProvider = launchSettingsProvider;
        }

        /// <summary>
        /// Import the LaunchTargetProviders which know how to run profiles
        /// </summary>
        [ImportMany]
        private OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider> ProfileLaunchTargetsProviders { get; set; }

        private ILaunchSettingsProvider LaunchSettingsProvider { get; }

        // Tracks the last launched provider so we can forward calls to IDeployedProjectItemMappingProvider
        public IDebugProfileLaunchTargetsProvider LastLaunchProvider { get; private set; }

        /// <summary>
        /// Called by CPS to determine whether we can launch
        /// </summary>
        public override Task<bool> CanLaunchAsync(DebugLaunchOptions launchOptions)
        {
            return TplExtensions.TrueTask;
        }

        /// <summary>
        /// Helper returns the correct debugger engine based on the targeted framework
        /// </summary>
        public static Guid GetManagedDebugEngineForFramework(string targetFramework)
        {
            // The engine depends on the framework
            if (IsDotNetCoreFramework(targetFramework))
            {
                return DebuggerEngines.ManagedCoreEngine;
            }
            else
            {
                return DebuggerEngines.ManagedOnlyEngine;
            }
        }

        /// <summary>
        /// TODO: This is a placeholder until issue https://github.com/dotnet/roslyn-project-system/issues/423 is addressed. 
        /// This information should come from the targets file.
        /// </summary>
        public static bool IsDotNetCoreFramework(string targetFramework)
        {
            const string NetStandardAppPrefix = ".NetStandardApp";
            const string NetStandardPrefix = ".NetStandard";
            const string NetCorePrefix = ".NetCore";
            const string NetCoreAppPrefix = ".NetCoreApp";
            return targetFramework.StartsWith(NetCoreAppPrefix, StringComparison.OrdinalIgnoreCase) ||
                   targetFramework.StartsWith(NetCorePrefix, StringComparison.OrdinalIgnoreCase) ||
                   targetFramework.StartsWith(NetStandardPrefix, StringComparison.OrdinalIgnoreCase) ||
                   targetFramework.StartsWith(NetStandardAppPrefix, StringComparison.OrdinalIgnoreCase);
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
            // Get the active debug profile (timeout of 5s, though in reality is should never take this long as even in error conditions
            // a snapshot is produced).
            var currentProfiles = await LaunchSettingsProvider.WaitForFirstSnapshot(5000).ConfigureAwait(false);
            ILaunchProfile activeProfile = currentProfiles?.ActiveProfile;

            // Should have a profile
            if (activeProfile == null)
            {
                throw new Exception(VSResources.ActiveLaunchProfileNotFound);
            }

            // Now find the DebugTargets provider for this profile
            var launchProvider = GetLaunchTargetsProvider(activeProfile) ??
                throw new Exception(string.Format(VSResources.DontKnowHowToRunProfile, activeProfile.Name));

            IReadOnlyList<IDebugLaunchSettings> launchSettings;
            if (fromDebugLaunch && launchProvider is IDebugProfileLaunchTargetsProvider2 launchProvider2)
            {
                launchSettings = await launchProvider2.QueryDebugTargetsForDebugLaunchAsync(launchOptions, activeProfile).ConfigureAwait(false);
            }
            else
            {
                launchSettings = await launchProvider.QueryDebugTargetsAsync(launchOptions, activeProfile).ConfigureAwait(false);
            }

            LastLaunchProvider = launchProvider;
            return launchSettings;
        }

        /// <summary>
        /// Returns the provider which knows how to launch the profile type.
        /// </summary>
        public IDebugProfileLaunchTargetsProvider GetLaunchTargetsProvider(ILaunchProfile profile)
        {
            // We search through the imports in order to find the one which supports the profile
            foreach (var provider in ProfileLaunchTargetsProviders)
            {
                if (provider.Value.SupportsProfile(profile))
                {
                    return provider.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Overridden to direct the launch to the current active provider as determined by the active launch profile
        /// </summary>
        public override async Task LaunchAsync(DebugLaunchOptions launchOptions)
        {
            var targets = await QueryDebugTargetsInternalAsync(launchOptions, fromDebugLaunch: true).ConfigureAwait(false);

            ILaunchProfile activeProfile = LaunchSettingsProvider.ActiveProfile;

            var targetProfile = GetLaunchTargetsProvider(activeProfile);
            if (targetProfile != null)
            {
                await targetProfile.OnBeforeLaunchAsync(launchOptions, activeProfile).ConfigureAwait(false);
            }

            await DoLaunchAsync(targets.ToArray()).ConfigureAwait(false);

            if (targetProfile != null)
            {
                await targetProfile.OnAfterLaunchAsync(launchOptions, activeProfile).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Launches the Visual Studio debugger.
        /// </summary>
        protected async Task<IReadOnlyList<VsDebugTargetProcessInfo>> DoLaunchAsync(params IDebugLaunchSettings[] launchSettings)
        {
            VsDebugTargetInfo4[] launchSettingsNative = launchSettings.Select(GetDebuggerStruct4).ToArray();
            if (launchSettingsNative.Length == 0)
            {
                return Array.Empty<VsDebugTargetProcessInfo>();
            }

            try
            {
                // The debugger needs to be called on the UI thread
                await ThreadingService.SwitchToUIThread();

                var shellDebugger = ServiceProvider.GetService(typeof(SVsShellDebugger)) as IVsDebugger4;
                var launchResults = new VsDebugTargetProcessInfo[launchSettingsNative.Length];
                shellDebugger.LaunchDebugTargets4((uint)launchSettingsNative.Length, launchSettingsNative, launchResults);
                return launchResults;
            }
            finally
            {
                // Free up the memory allocated to the (mostly) managed debugger structure.
                foreach (var nativeStruct in launchSettingsNative)
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
            var debugInfo = new VsDebugTargetInfo4();

            // **Begin common section -- keep this in sync with GetDebuggerStruct**
            debugInfo.dlo = (uint)info.LaunchOperation;
            debugInfo.LaunchFlags = (uint)info.LaunchOptions;
            debugInfo.bstrRemoteMachine = info.RemoteMachine;
            debugInfo.bstrArg = info.Arguments;
            debugInfo.bstrCurDir = info.CurrentDirectory;
            debugInfo.bstrExe = info.Executable;

            debugInfo.bstrEnv = GetSerializedEnvironmentString(info.Environment);
            debugInfo.guidLaunchDebugEngine = info.LaunchDebugEngineGuid;

            var guids = new List<Guid>(1);
            guids.Add(info.LaunchDebugEngineGuid);
            if (info.AdditionalDebugEngines != null)
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
            debugInfo.fSendToOutputWindow = info.SendToOutputWindow ? 1 : 0;
            debugInfo.dwProcessId = unchecked((uint)info.ProcessId);
            debugInfo.pUnknown = info.Unknown;
            debugInfo.guidProcessLanguage = info.ProcessLanguageGuid;

            // **End common section**

            if (info.StandardErrorHandle != IntPtr.Zero || info.StandardInputHandle != IntPtr.Zero || info.StandardOutputHandle != IntPtr.Zero)
            {
                var processStartupInfo = new VsDebugStartupInfo
                {
                    hStdInput = unchecked((uint)info.StandardInputHandle.ToInt32()),
                    hStdOutput = unchecked((uint)info.StandardOutputHandle.ToInt32()),
                    hStdError = unchecked((uint)info.StandardErrorHandle.ToInt32()),
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
        /// key=value/0key2=value2/0/0, with null's between each entry and a double null terminator.
        /// </summary>
        private static string GetSerializedEnvironmentString(IDictionary<string, string> environment)
        {
            // If no dictionary was set, or its empty, the debugger wants null for its environment block.
            if (environment == null || environment.Count == 0)
            {
                return null;
            }

            // Collect all the variables as a null delimited list of key=value pairs.
            var result = new StringBuilder();
            foreach (var pair in environment)
            {
                result.Append(pair.Key);
                result.Append('=');
                result.Append(pair.Value);
                result.Append('\0');
            }

            // Add a final list-terminating null character.
            // This is sent to native code as a BSTR and no null is added automatically.
            // But the contract of the format of the data requires that this be a null-delimited,
            // null-terminated list.
            result.Append('\0');
            return result.ToString();
        }

        /// <summary>
        /// Collects an array of GUIDs into an array of bytes.
        /// </summary>
        /// <remarks>
        /// The order of the GUIDs are preserved, and each GUID is copied exactly one after the other in the byte array.
        /// </remarks>
        private static byte[] GetGuidBytes(IList<Guid> guids)
        {
            int sizeOfGuid = Guid.Empty.ToByteArray().Length;
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
        /// Implementd so that we can map URL's back to local file item paths
        /// </summary>
        public bool TryGetProjectItemPathFromDeployedPath(string deployedPath, out string localPath)
        {
            // Just delegate to the last provider. It needs to figure out how best to map the items
            localPath = null;
            if (LastLaunchProvider is IDeployedProjectItemMappingProvider deployedItemMapper)
            {
                return deployedItemMapper.TryGetProjectItemPathFromDeployedPath(deployedPath, out localPath);
            }

            // Return false to allow normal processing
            return false;
        }
    }
}
