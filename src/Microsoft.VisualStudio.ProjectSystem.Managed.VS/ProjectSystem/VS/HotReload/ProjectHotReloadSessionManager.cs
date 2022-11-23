// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    [Export(typeof(IProjectHotReloadSessionManager))]
    [Export(typeof(IProjectHotReloadUpdateApplier))]
    internal class ProjectHotReloadSessionManager : OnceInitializedOnceDisposedAsync, IProjectHotReloadSessionManager, IProjectHotReloadUpdateApplier
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;
        private readonly IActiveDebugFrameworkServices _activeDebugFrameworkServices;
        private readonly Lazy<IProjectHotReloadAgent> _projectHotReloadAgent;
        private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadDiagnosticOutputService;

        // Protect the state from concurrent access. For example, our Process.Exited event
        // handler may run on one thread while we're still setting up the session on
        // another. To ensure consistent and proper behavior we need to serialize access.
        private readonly AsyncSemaphore _semaphore = new(initialCount: 1);

        private readonly Dictionary<int, HotReloadState> _activeSessions = new();
        private HotReloadState? _pendingSessionState = null;
        private int _nextUniqueId = 1;

        public bool HasActiveHotReloadSessions => _activeSessions.Count != 0;

        [ImportingConstructor]
        public ProjectHotReloadSessionManager(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            IProjectFaultHandlerService projectFaultHandlerService,
            IActiveDebugFrameworkServices activeDebugFrameworkServices,
            Lazy<IProjectHotReloadAgent> projectHotReloadAgent,
            Lazy<IHotReloadDiagnosticOutputService> hotReloadDiagnosticOutputService)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _projectFaultHandlerService = projectFaultHandlerService;
            _activeDebugFrameworkServices = activeDebugFrameworkServices;
            _projectHotReloadAgent = projectHotReloadAgent;
            _hotReloadDiagnosticOutputService = hotReloadDiagnosticOutputService;
        }

        public async Task ActivateSessionAsync(int processId, bool runningUnderDebugger, string projectName)
        {
            using AsyncSemaphore.Releaser semaphoreReleaser = await _semaphore.EnterAsync();

            if (_pendingSessionState is not null)
            {
                Assumes.NotNull(_pendingSessionState.Session);

                try
                {
                    Process? process = Process.GetProcessById(processId);

                    WriteOutputMessage(
                        new HotReloadLogMessage(
                            HotReloadVerbosity.Detailed,
                            VSResources.ProjectHotReloadSessionManager_AttachingToProcess,
                            projectName,
                            _pendingSessionState.Session.Name,
                            (uint)processId,
                            HotReloadDiagnosticErrorLevel.Info
                        ),
                        default);

                    process.Exited += _pendingSessionState.OnProcessExited;
                    process.EnableRaisingEvents = true;

                    if (process.HasExited)
                    {
                        WriteOutputMessage(
                            new HotReloadLogMessage(
                                HotReloadVerbosity.Detailed,
                                VSResources.ProjectHotReloadSessionManager_ProcessAlreadyExited,
                                projectName,
                                _pendingSessionState.Session.Name,
                                (uint)processId,
                                HotReloadDiagnosticErrorLevel.Info
                            ),
                            default);

                        process.Exited -= _pendingSessionState.OnProcessExited;
                        process = null;
                    }

                    _pendingSessionState.Process = process;
                }
                catch (Exception ex)
                {
                    WriteOutputMessage(
                        new HotReloadLogMessage(
                            HotReloadVerbosity.Minimal,
                            $"${ex.GetType()}: ${ex.Message}",
                            projectName,
                            _pendingSessionState.Session.Name,
                            (uint)processId,
                            HotReloadDiagnosticErrorLevel.Error
                        ),
                        default);
                }

                if (_pendingSessionState.Process is null)
                {
                    WriteOutputMessage(
                        new HotReloadLogMessage(
                            HotReloadVerbosity.Minimal,
                            VSResources.ProjectHotReloadSessionManager_NoActiveProcess,
                            projectName,
                            _pendingSessionState.Session.Name,
                            (uint)processId,
                            HotReloadDiagnosticErrorLevel.Warning
                        ),
                        default);
                }
                else
                {
                    await _pendingSessionState.Session.StartSessionAsync(runningUnderDebugger, cancellationToken: default);
                    _activeSessions.Add(processId, _pendingSessionState);
                }

                _pendingSessionState = null;
            }
        }

        public async Task<bool> TryCreatePendingSessionAsync(IDictionary<string, string> environmentVariables)
        {
            using AsyncSemaphore.Releaser semaphoreReleaser = await _semaphore.EnterAsync();

            if (await DebugFrameworkSupportsHotReloadAsync()
                && await GetDebugFrameworkVersionAsync() is string frameworkVersion
                && !string.IsNullOrWhiteSpace(frameworkVersion))
            {
                if (await DebugFrameworkSupportsStartupHooksAsync())
                {
                    string name = Path.GetFileNameWithoutExtension(_project.FullPath);
                    HotReloadState state = new(this);
                    IProjectHotReloadSession? projectHotReloadSession = _projectHotReloadAgent.Value.CreateHotReloadSession(name, _nextUniqueId++, frameworkVersion, state);

                    if (projectHotReloadSession is not null)
                    {
                        state.Session = projectHotReloadSession;
                        await projectHotReloadSession.ApplyLaunchVariablesAsync(environmentVariables, default);
                        _pendingSessionState = state;

                        return true;
                    }
                }
                else
                {
                    // If startup hooks are not supported then tell the user why Hot Reload isn't available.
                    string projectName = Path.GetFileNameWithoutExtension(_project.FullPath);
                    
                    WriteOutputMessage(
                        new HotReloadLogMessage(
                            HotReloadVerbosity.Minimal,
                            VSResources.ProjectHotReloadSessionManager_StartupHooksDisabled,
                            projectName,
                            null,
                            HotReloadDiagnosticOutputService.GetProcessId(),
                            HotReloadDiagnosticErrorLevel.Warning
                        ),
                        default);
                }
            }

            _pendingSessionState = null;
            return false;
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            using AsyncSemaphore.Releaser semaphoreRelease = await _semaphore.EnterAsync();

            foreach (HotReloadState sessionState in _activeSessions.Values)
            {
                Assumes.NotNull(sessionState.Process);
                Assumes.NotNull(sessionState.Session);

                sessionState.Process.Exited -= sessionState.OnProcessExited;
                _projectFaultHandlerService.Forget(sessionState.Session.StopSessionAsync(default), _project);
            }

            _activeSessions.Clear();
        }

        private void WriteOutputMessage(HotReloadLogMessage hotReloadLogMessage, CancellationToken cancellationToken) => _hotReloadDiagnosticOutputService.Value.WriteLine(hotReloadLogMessage, cancellationToken);

        /// <summary>
        /// Checks if the project configuration targeted for debugging/launch meets the
        /// basic requirements to support Hot Reload. Note that there may be other, specific
        /// settings that prevent the use of Hot Reload even if the basic requirements are met.
        /// </summary>
        private async Task<bool> DebugFrameworkSupportsHotReloadAsync()
        {
            return await ConfiguredProjectForDebugHasHotReloadCapabilityAsync()
                && await DebugSymbolsEnabledInConfiguredProjectForDebugAsync()
                && !await OptimizeEnabledInConfiguredProjectForDebugAsync();
        }

        private Task<ConfiguredProject?> GetConfiguredProjectForDebugAsync() =>
            _activeDebugFrameworkServices.GetConfiguredProjectForActiveFrameworkAsync();

        private async Task<string?> GetDebugFrameworkVersionAsync()
        {
            if (await GetPropertyFromDebugFrameworkAsync(ConfigurationGeneral.TargetFrameworkVersionProperty) is string targetFrameworkVersion)
            {
                if (targetFrameworkVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    targetFrameworkVersion = targetFrameworkVersion.Substring(startIndex: 1);
                }

                return targetFrameworkVersion;
            }

            return null;
        }

        /// <summary>
        /// Returns whether or not the project configuration targeted for debugging/launch
        /// supports startup hooks. These are used to start Hot Reload in the launched
        /// process.
        /// </summary>
        private async Task<bool> DebugFrameworkSupportsStartupHooksAsync()
        {
            if (await GetPropertyFromDebugFrameworkAsync(ConfigurationGeneral.StartupHookSupportProperty) is string startupHookSupport)
            {
                return !StringComparers.PropertyLiteralValues.Equals(startupHookSupport, "false");
            }

            return true;
        }

        /// <summary>
        /// Returns whether or not the project configuration targeted for debugging/launch
        /// optimizes binaries. Defaults to false if the property is not defined.
        /// </summary>
        private async Task<bool> OptimizeEnabledInConfiguredProjectForDebugAsync()
        {
            if (await GetPropertyFromDebugFrameworkAsync("Optimize") is string optimize)
            {
                return StringComparers.PropertyLiteralValues.Equals(optimize, "true");
            }

            return false;
        }

        /// <summary>
        /// Returns whether or not the project configuration targeted for debugging/launch
        /// emits debug symbols. Defaults to false if the property is not defined.
        /// </summary>
        private async Task<bool> DebugSymbolsEnabledInConfiguredProjectForDebugAsync()
        {
            if (await GetPropertyFromDebugFrameworkAsync("DebugSymbols") is string debugSymbols)
            {
                return StringComparers.PropertyLiteralValues.Equals(debugSymbols, "true");
            }

            return false;
        }

        private async Task<bool> ConfiguredProjectForDebugHasHotReloadCapabilityAsync()
        {
            ConfiguredProject? configuredProjectForDebug = await GetConfiguredProjectForDebugAsync();
            if (configuredProjectForDebug is null)
            {
                return false;
            }

            return configuredProjectForDebug.Capabilities.AppliesTo("SupportsHotReload");
        }

        private async Task<string?> GetPropertyFromDebugFrameworkAsync(string propertyName)
        {
            ConfiguredProject? configuredProjectForDebug = await GetConfiguredProjectForDebugAsync();
            if (configuredProjectForDebug is null)
            {
                return null;
            }

            Assumes.Present(configuredProjectForDebug.Services.ProjectPropertiesProvider);
            IProjectProperties commonProperties = configuredProjectForDebug.Services.ProjectPropertiesProvider.GetCommonProperties();
            string propertyValue = await commonProperties.GetEvaluatedPropertyValueAsync(propertyName);

            return propertyValue;
        }

        private void OnProcessExited(HotReloadState hotReloadState)
        {
            _projectFaultHandlerService.Forget(OnProcessExitedAsync(hotReloadState), _project);
        }

        private async Task OnProcessExitedAsync(HotReloadState hotReloadState)
        {
            Assumes.NotNull(hotReloadState.Session);
            Assumes.NotNull(hotReloadState.Process);

            WriteOutputMessage(
                new HotReloadLogMessage(
                    HotReloadVerbosity.Minimal,
                    VSResources.ProjectHotReloadSessionManager_ProcessExited,
                    hotReloadState.Session?.Name,
                    (_nextUniqueId - 1).ToString(),
                    HotReloadDiagnosticOutputService.GetProcessId(hotReloadState.Process),
                    HotReloadDiagnosticErrorLevel.Info
                ),
                default);
            
            await StopProjectAsync(hotReloadState, default);

            hotReloadState.Process.Exited -= hotReloadState.OnProcessExited;
        }

        private static IDeltaApplier? GetDeltaApplier(HotReloadState hotReloadState)
        {
            return null;
        }

        private static Task<bool> RestartProjectAsync(HotReloadState hotReloadState, CancellationToken cancellationToken)
        {
            // TODO: Support restarting the project.
            return TaskResult.False;
        }

        private static Task OnAfterChangesAppliedAsync(HotReloadState hotReloadState, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task<bool> StopProjectAsync(HotReloadState hotReloadState, CancellationToken cancellationToken)
        {
            Assumes.NotNull(hotReloadState.Session);
            Assumes.NotNull(hotReloadState.Process);

            using AsyncSemaphore.Releaser semaphoreReleaser = await _semaphore.EnterAsync();

            try
            {
                if (_activeSessions.Remove(hotReloadState.Process.Id))
                {
                    await hotReloadState.Session.StopSessionAsync(cancellationToken);

                    if (!hotReloadState.Process.HasExited)
                    {
                        // First try to close the process nicely and if that doesn't work kill it.
                        if (!hotReloadState.Process.CloseMainWindow())
                        {
                            hotReloadState.Process.Kill();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteOutputMessage(
                    new HotReloadLogMessage(
                        HotReloadVerbosity.Minimal,
                        string.Format(VSResources.ProjectHotReloadSessionManager_ErrorStoppingTheSession, ex.GetType(), ex.Message),
                        hotReloadState.Session?.Name,
                        null,
                        HotReloadDiagnosticOutputService.GetProcessId(hotReloadState.Process),
                        HotReloadDiagnosticErrorLevel.Error
                    ),
                    cancellationToken);
            }

            return true;
        }

        /// <inheritdoc />
        public async Task ApplyHotReloadUpdateAsync(Func<IDeltaApplier, CancellationToken, Task> applyFunction, CancellationToken cancelToken)
        {
            using AsyncSemaphore.Releaser semaphoreRelease = await _semaphore.EnterAsync(cancelToken);

            // Run the updates in parallel
            List<Task> updateTasks = new List<Task>();
            foreach (HotReloadState sessionState in _activeSessions.Values)
            {
                cancelToken.ThrowIfCancellationRequested();
                if (sessionState.Session is IProjectHotReloadSessionInternal sessionInternal)
                {
                    IDeltaApplier? deltaApplier = sessionInternal.DeltaApplier;
                    if (deltaApplier is not null)
                    {
                        updateTasks.Add(applyFunction(deltaApplier, cancelToken));
                    }
                }
            }
            
            // Wait for their completion
            if (updateTasks.Count > 0)
            {
                await Task.WhenAll(updateTasks);
            }
        }

        private class HotReloadState : IProjectHotReloadSessionCallback
        {
            private readonly ProjectHotReloadSessionManager _sessionManager;

            public Process? Process { get; set; }
            public IProjectHotReloadSession? Session { get; set; }

            // TODO: Support restarting the session.
            public bool SupportsRestart => false;

            public HotReloadState(ProjectHotReloadSessionManager sessionManager)
            {
                _sessionManager = sessionManager;
            }

            internal void OnProcessExited(object sender, EventArgs e)
            {
                _sessionManager.OnProcessExited(this);
            }

            public Task OnAfterChangesAppliedAsync(CancellationToken cancellationToken)
            {
                return ProjectHotReloadSessionManager.OnAfterChangesAppliedAsync(this, cancellationToken);
            }

            public Task<bool> StopProjectAsync(CancellationToken cancellationToken)
            {
                return _sessionManager.StopProjectAsync(this, cancellationToken);
            }

            public Task<bool> RestartProjectAsync(CancellationToken cancellationToken)
            {
                return ProjectHotReloadSessionManager.RestartProjectAsync(this, cancellationToken);
            }

            public IDeltaApplier? GetDeltaApplier()
            {
                return ProjectHotReloadSessionManager.GetDeltaApplier(this);
            }
        }
    }
}
