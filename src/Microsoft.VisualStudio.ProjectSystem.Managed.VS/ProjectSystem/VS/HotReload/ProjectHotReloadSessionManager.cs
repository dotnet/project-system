// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    [Export(typeof(IProjectHotReloadSessionManager))]
    internal class ProjectHotReloadSessionManager : IProjectHotReloadSessionManager
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;
        private readonly IActiveDebugFrameworkServices _activeDebugFrameworkServices;
        private readonly Lazy<IProjectHotReloadAgent> _projectHotReloadAgent;
        private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadDiagnosticOutputService;

        private HotReloadState? _pendingSessionState = null;

        private ImmutableDictionary<int, HotReloadState> _activeSessions = ImmutableDictionary<int, HotReloadState>.Empty;

        private int _nextUniqueId = 1;

        [ImportingConstructor]
        public ProjectHotReloadSessionManager(
            UnconfiguredProject project,
            IProjectFaultHandlerService projectFaultHandlerService,
            IActiveDebugFrameworkServices activeDebugFrameworkServices,
            Lazy<IProjectHotReloadAgent> projectHotReloadAgent,
            Lazy<IHotReloadDiagnosticOutputService> hotReloadDiagnosticOutputService)
        {
            _project = project;
            _projectFaultHandlerService = projectFaultHandlerService;
            _activeDebugFrameworkServices = activeDebugFrameworkServices;
            _projectHotReloadAgent = projectHotReloadAgent;
            _hotReloadDiagnosticOutputService = hotReloadDiagnosticOutputService;
        }

        public async Task ActivateSessionAsync(int processId, bool runningUnderDebugger)
        {
            if (_pendingSessionState is not null)
            {
                Assumes.NotNull(_pendingSessionState.Session);

                try
                {
                    Process? process = Process.GetProcessById(processId);

                    await _hotReloadDiagnosticOutputService.Value.WriteLineAsync($"{_pendingSessionState.Session.Name}: Attaching to process '{processId}'.");

                    process.Exited += _pendingSessionState.OnProcessExited;
                    process.EnableRaisingEvents = true;

                    if (process.HasExited)
                    {
                        await _hotReloadDiagnosticOutputService.Value.WriteLineAsync($"{_pendingSessionState.Session.Name}: The process has already exited.");
                        process.Exited -= _pendingSessionState.OnProcessExited;
                        process = null;
                    }

                    _pendingSessionState.Process = process;
                }
                catch (Exception ex)
                {
                    await _hotReloadDiagnosticOutputService.Value.WriteLineAsync($"{_pendingSessionState.Session.Name}: Error while attaching to process '{processId}':\r\n{ex.GetType()}\r\n{ex.Message}");
                }

                if (_pendingSessionState.Process is null)
                {
                    await _hotReloadDiagnosticOutputService.Value.WriteLineAsync($"{_pendingSessionState.Session.Name}: Unable to start Hot Reload session: no active process.");
                }
                else
                {
                    _ = _pendingSessionState.Session.StartSessionAsync(runningUnderDebugger, cancellationToken: default);
                    ImmutableInterlocked.TryAdd(ref _activeSessions, processId, _pendingSessionState);
                }

                _pendingSessionState = null;
            }
        }

        public async Task<bool> TryCreatePendingSessionAsync(IDictionary<string, string> environmentVariables)
        {
            if (await DebugFrameworkSupportsHotReloadAsync()
                && await GetDebugFrameworkVersionAsync() is string frameworkVersion)
            {
                string name = $"{Path.GetFileNameWithoutExtension(_project.FullPath)}:{_nextUniqueId++}";
                HotReloadState state = new(this);
                IProjectHotReloadSession? projectHotReloadSession = _projectHotReloadAgent.Value.CreateHotReloadSession(name, frameworkVersion, state);

                if (projectHotReloadSession is not null)
                {
                    state.Session = projectHotReloadSession;
                    await projectHotReloadSession.ApplyLaunchVariablesAsync(environmentVariables, default);
                    _pendingSessionState = state;

                    return true;
                }
            }

            _pendingSessionState = null;
            return false;
        }

        private async Task<bool> DebugFrameworkSupportsHotReloadAsync()
        {
            ConfiguredProject? configuredProjectForDebug = await GetConfiguredProjectForDebugAsync();
            if (configuredProjectForDebug is null)
            {
                return false;
            }

            return configuredProjectForDebug.Capabilities.AppliesTo("SupportsHotReload");
        }

        private Task<ConfiguredProject?> GetConfiguredProjectForDebugAsync() =>
            _activeDebugFrameworkServices.GetConfiguredProjectForActiveFrameworkAsync();

        private async Task<string?> GetDebugFrameworkVersionAsync()
        {
            ConfiguredProject? configuredProjectForDebug = await GetConfiguredProjectForDebugAsync();
            if (configuredProjectForDebug is null)
            {
                return null;
            }

            Assumes.Present(configuredProjectForDebug.Services.ProjectPropertiesProvider);
            IProjectProperties commonProperties = configuredProjectForDebug.Services.ProjectPropertiesProvider.GetCommonProperties();
            string targetFrameworkVersion = await commonProperties.GetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetFrameworkVersionProperty);

            if (targetFrameworkVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                targetFrameworkVersion = targetFrameworkVersion.Substring(startIndex: 1);
            }

            return targetFrameworkVersion;
        }

        private void OnProcessExited(HotReloadState hotReloadState)
        {
            _projectFaultHandlerService.Forget(OnProcessExitedAsync(hotReloadState), _project);
        }

        private async Task OnProcessExitedAsync(HotReloadState hotReloadState)
        {
            Assumes.NotNull(hotReloadState.Session);
            Assumes.NotNull(hotReloadState.Process);

            await _hotReloadDiagnosticOutputService.Value.WriteLineAsync($"{hotReloadState.Session.Name}: The process has exited.");

            await StopProjectAsync(hotReloadState, default);
        }

        private IDeltaApplier? GetDeltaApplier(HotReloadState hotReloadState)
        {
            return null;
        }

        private Task<bool> RestartProjectAsync(HotReloadState hotReloadState, CancellationToken cancellationToken)
        {
            // TODO: Support restarting the project.
            return TaskResult.False;
        }

        private Task OnAfterChangesAppliedAsync(HotReloadState hotReloadState, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task<bool> StopProjectAsync(HotReloadState hotReloadState, CancellationToken cancellationToken)
        {
            Assumes.NotNull(hotReloadState.Session);
            Assumes.NotNull(hotReloadState.Process);

            try
            {
                if (ImmutableInterlocked.TryRemove(ref _activeSessions, hotReloadState.Process.Id, out _))
                {
                    await hotReloadState.Session.StopSessionAsync(cancellationToken);

                    // First try to close the process nicely and if that doesn't work kill it.
                    if (!hotReloadState.Process.CloseMainWindow())
                    {
                        hotReloadState.Process.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                await _hotReloadDiagnosticOutputService.Value.WriteLineAsync($"{hotReloadState.Session.Name}: Error while stopping the session:\r\n{ex.GetType()}\r\n{ex.Message}");
            }

            return true;
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
                return _sessionManager.OnAfterChangesAppliedAsync(this, cancellationToken);
            }

            public Task<bool> StopProjectAsync(CancellationToken cancellationToken)
            {
                return _sessionManager.StopProjectAsync(this, cancellationToken);
            }

            public Task<bool> RestartProjectAsync(CancellationToken cancellationToken)
            {
                return _sessionManager.RestartProjectAsync(this, cancellationToken);
            }

            public IDeltaApplier? GetDeltaApplier()
            {
                return _sessionManager.GetDeltaApplier(this);
            }
        }
    }
}
