// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web
{
    /// <summary>
    /// Provides QueryDebugTargetsAsync() support for web profile iisexpress, web command, IIS. It is 
    /// not an exported CPS debugger but is imported by ProjectDebuggerProvider to handle IIS
    /// and IIS Express web servers.
    /// </summary>
    [Export(typeof(IDebugProfileLaunchTargetsProvider))]
    [AppliesTo(ProjectCapability.AspNetLaunchProfiles)]
    [Order(100)]
    internal class WebDebugTargetsProvider : IDebugProfileLaunchTargetsProvider, IDebugProfileLaunchTargetsProvider2, IDeployedProjectItemMappingProvider, IDisposable
    {
        private readonly ConfiguredProject _configuredProject;
        private readonly IDebugTokenReplacer _tokenReplacer;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;

        [ImportingConstructor]
        public WebDebugTargetsProvider(ConfiguredProject configuredProject,
                                       IDebugTokenReplacer tokenReplacer,
                                       IUnconfiguredProjectVsServices projectVsServices)
        {
            _configuredProject = configuredProject;
            _tokenReplacer = tokenReplacer;
            _projectVsServices = projectVsServices;
        }

        public int ProcessIDToResume { get; protected set; }

        // Prevents the securtiy warning dialog from being shown when attaching to IIS
        public const uint DBGLAUNCH_BypassAttachSecurity = 0x10000000;

        /// <summary>
        /// This is set to the URL(s) we are going to debug. There could be more than one in the case of a site with a
        /// secure and non-secure port binding. This is important for mapping urls to project items and is handled by the ProjectDebugerProvider
        /// </summary>
        public List<string> DebuggingRootUrls { get; } = new List<string>();

        /// <summary>
        /// Supports the Project profile.
        /// </summary>
        public bool SupportsProfile(ILaunchProfile profile)
        {
            return string.Equals(profile.CommandName, LaunchSettingsProvider.RunProjectCommandName, StringComparisons.LaunchProfileCommandNames);
        }

        public Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
        {
            return Task.CompletedTask;
        }

        public Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile)
        {
            return QueryDebugTargetsAsync(launchOptions, activeProfile, isQuery: true);
        }

        public Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsForDebugLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile)
        {
            return QueryDebugTargetsAsync(launchOptions, activeProfile, isQuery: false);
        }

        /// <summary>
        /// If isQuery is true, this is being called to just get the list of targets and not actually start any webservers
        /// </summary>
        private async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile, bool isQuery)
        {
            var currentConfiguration = GetConfigurationName(_configuredProject.ProjectConfiguration);

            activeProfile = await _tokenReplacer.ReplaceTokensInProfileAsync(activeProfile);

            //// Get the appropriate bitness for the server
            //var serverBitness = activeProfile.GetServerBitness();

            //// Get all the server urls
            //var serverUrls = activeWebServer.GetServerUrls(launchSettings: activeWebServerData.Item2, resolvedProfile: activeProfile);

            //Uri browserUri = null;
            //if (activeProfile.LaunchBrowser)
            //{
            //    browserUri = activeProfile.GetEffectiveLaunchUri(serverUrls.FirstOrDefault());
            //}

            //var properties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
            //var activeFramework = await properties.GetEvaluatedPropertyValueAsync(ProjectFileProperty.TargetFrameworkMoniker);

            var launchTargets = new List<DebugLaunchSettings>();

            //// For queries we don't want to start anything
            //if (isQuery)
            //{
            //    // Only profiling and debugging involve adding a launch target. 
            //    if (launchOptions.IsDebugging() || launchOptions.IsProfiling())
            //    {
            //        // Get the expected launch process
            //        var launchData = await activeWebServer.GetLaunchInformationAsync(activeFramework, activeProfile);

            //        // Since we don't know the pid yet, we pass 0 
            //        var serverSettings = GetDebugLaunchSettings(launchOptions, 0, launchData.Item1, launchData.Item3, activeProfile, activeFramework);
            //        launchTargets.Add(serverSettings);
            //    }
            //}
            //else
            //{
            //    // Start the server (throws on failure)
            //    await WebServerStateManager.LaunchWebServerAsync(
            //        activeWebServer,
            //        activeFramework,
            //        activeProfile,
            //        currentConfiguration,
            //        false,
            //        launchOptions.ToServerLaunchOptions(),
            //        true,
            //        serverBitness);

            //    perfEvent?.AddPerformanceProperty(TelemetryExtensions.HostServerLaunchElapsedTimeProperty);

            //    // If we aren't debugging or profiling, we don't create server settings option. If we do so, the debugger will ignore the AlreadyRunning and
            //    // start the proces anyway
            //    if (launchOptions.IsDebugging() || launchOptions.IsProfiling())
            //    {
            //        // Get the list of processes to attach to (can be more than one but we currently only support one).
            //        var processInfo = await activeWebServer.GetDebuggingProcessInformationAsync();
            //        System.Diagnostics.Debug.Assert(processInfo.Count == 1);

            //        perfEvent?.AddPerformanceProperty(TelemetryExtensions.DiscoverPidElapsedTimeProperty);

            //        // Dotnet cli the process is started in a suspended state. We need to remember that so that we can resume the process once
            //        // debugger has attached. The suspended state is indicated by Item3. Profiling doesn't support the following
            //        ProcessIDToResume = processInfo[0].Item3 == DebuggingOptions.RequiresResume ? processInfo[0].Item2 : 0;

            //        var serverSettings = GetDebugLaunchSettings(launchOptions, processInfo[0].Item2, processInfo[0].Item1, processInfo[0].Item3, activeProfile, activeFramework);

            //        launchTargets.Add(serverSettings);
            //    }
            //}

            // We always set the debugging root url to the url of the server - not the url being launched as it could be anything. If we 
            // have secure bindings, we add that one too.
            // Set the debugging root urls to the urls currently supported by the server - not the url being launched as it could be anything
            //foreach (var url in serverUrls)
            //{
            //    DebuggingRootUrls.Add(url.EnsureTrailingChar('/'));
            //}

            //// If in nexus (server mode) we ignore doing anything else with the browsers at this point. However, if launch browser is enabled we still
            //// need to call the client to launch browsers so we need to remember the uri to launch
            //if (activeProfile.LaunchBrowser)
            //{
            //    if (await VsWrappers.GetVsShellWrapper().IsServerModeAsync())
            //    {
            //        BrowserUriToLaunchAfterResume = browserUri;
            //        CancellationTokenSource = new CancellationTokenSource();
            //    }
            //    else
            //    {
            //        var staticAssets = await WebProjectInformation.GetStaticAssetsAsync();
            //        var browserTargets = await BrowserLaunchHelper.GetAllBrowserLaunchTargetsAsync(browserUri.AbsoluteUri, WebProjectInformation.WebRoot, GetProjectWrapper(),
            //                                                                            launchOptions, activeProfile.GetInspectUri(), staticAssets, isQuery);

            //        if (!isQuery && launchOptions.IsDebugging())
            //        {
            //            DebugTargetsObserver.RequestDebugTargetsObservation(browserTargets, ProjectServices.Project.FullPath);
            //        }

            //        if (VsWrappers.GetVsShellWrapper().IsVsRunningElevated())
            //        {
            //            // Note that if we are debugging and and this is in-process, we must delay starting chrome browsers until after the debugger
            //            // is attached. This allows startup code to be debugged. Since, it is okay to do this in all cases, we just simplify to always
            //            // launching them after the attach
            //            BrowserTargetsToLaunchAfterResume = BrowserLaunchHelper.RemoveChromeBrowsersNotBeingDebugged(browserTargets);
            //            if (isQuery)
            //            {
            //                // We don't actually need to do this if a query
            //                BrowserTargetsToLaunchAfterResume = null;
            //            }
            //        }

            //        launchTargets.AddRange(browserTargets);
            //    }
            //}

            //// Do we have async work? If so create a cancellation token
            //if (ProcessIDToResume != 0 || BrowserUriToLaunchAfterResume != null || BrowserTargetsToLaunchAfterResume != null)
            //{
            //    CancellationTokenSource = new CancellationTokenSource();
            //}

            //WebServerUsedForLaunch = activeWebServer;

            return launchTargets.ToArray();
        }

        private static string GetConfigurationName(ProjectConfiguration cfg)
        {
            if (cfg.Dimensions.TryGetValue("Configuration", out var cfgValue))
            {
                return cfgValue;
            }

            return "Debug";
        }

        /// <summary>
        /// Helper to populate the debug launch settings.
        /// </summary>
        private DebugLaunchSettings GetDebugLaunchSettings(DebugLaunchOptions launchOptions, int pid, string processExe, ILaunchProfile activeProfile, string activeFramework)
        {
            var serverSettings = new DebugLaunchSettings(launchOptions)
            {
                Project = _projectVsServices.VsHierarchy,

                // Work around bug that profiling doesn't send the profiling flag with queries
                // serverSettings.LaunchOperation = launchOptions.IsProfiling()? DebugLaunchOperation.AlreadyRunning : DebugLaunchOperation.AttachToSuspendedLaunchProcess;
                LaunchOperation = DebugLaunchOperation.AlreadyRunning,
                ProcessId = pid,
                Executable = processExe,
                LaunchOptions = launchOptions |
                                            DebugLaunchOptions.WaitForAttachComplete |
                                            DebugLaunchOptions.StopDebuggingOnEnd
            };

            //if (!otherDebuggingOptions.HasFlag(DebuggingOptions.DetachOnStop))
            //{
            //    serverSettings.LaunchOptions |= DebugLaunchOptions.TerminateOnStop;
            //}

            //if (activeProfile.IsIIS())
            //{
            //    serverSettings.LaunchOptions |= (DebugLaunchOptions)DBGLAUNCH_BypassAttachSecurity;
            //}

            //serverSettings.LaunchDebugEngineGuid = DebugExtensions.GetManagedDebugEngineForFramework(activeFramework);
            //if (activeProfile.NativeDebuggingIsEnabled())
            //{
            //    serverSettings.AdditionalDebugEngines.Add(DebuggerEngines.NativeOnlyEngine);
            //}

            //if (activeProfile.SqlDebuggingIsEnabled())
            //{
            //    serverSettings.AdditionalDebugEngines.Add(DebuggerEngines.SqlEngine);
            //}
            return serverSettings;
        }

        /// <summary>
        /// IDeployedProjectItemMappingProvider
        /// Implementd so that we can map URL's back to local file item paths. ProjectDebuggerProvider will forward mapping requests
        /// from CPS to us if we are the active launch provider
        /// 
        /// Note that this is called via IsDocumentInProject() so there is an expectation that this function only returns success
        /// for file items. 
        /// </summary>
        public bool TryGetProjectItemPathFromDeployedPath(string deployedPath, out string? localPath)
        {
            localPath = null;
            foreach (var url in DebuggingRootUrls)
            {
                System.Diagnostics.Debug.Assert(url.EndsWith("/", StringComparison.OrdinalIgnoreCase));
                if (deployedPath.StartsWith(url, StringComparison.OrdinalIgnoreCase))
                {
                    // Figure out the part of the url after the projectUrl and append it to where the web root folder is - which
                    // may be different than the project root.
                    // We need to remove any query string or bookmark so leverage the uri class to extract everything up to and including the path, 
                    // subtract the application url, and combine what is left with the web root path.
                    var uri = new Uri(deployedPath);
                    var relPath = uri.GetLeftPart(UriPartial.Path).Substring(url.Length).Replace('/', '\\');
                    if (relPath.StartsWith("\\", StringComparison.OrdinalIgnoreCase))
                    {
                        relPath = relPath.Substring(1);
                    }

                    localPath = Path.Combine(Path.GetDirectoryName(_projectVsServices.Project.FullPath), relPath);
                    return File.Exists(localPath);
                }
            }

            // Return false to allow normal processing
            return false;
        }

        public void Dispose()
        {
        }
    }
}
