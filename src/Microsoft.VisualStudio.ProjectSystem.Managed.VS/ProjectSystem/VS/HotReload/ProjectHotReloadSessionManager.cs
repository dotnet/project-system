// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
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

        // Protect the state from concurrent access. For example, our Process.Exited event
        // handler may run on one thread while we're still setting up the session on
        // another. To ensure consistent and proper behavior we need to serialize access.
        private readonly AsyncSemaphore _semaphore = new(initialCount: 1);

        private readonly Dictionary<int, HotReloadState> _activeSessions = new();
        private HotReloadState? _pendingSessionState = null;
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
            using AsyncSemaphore.Releaser semaphoreReleaser = await _semaphore.EnterAsync();

            if (_pendingSessionState is not null)
            {
                Assumes.NotNull(_pendingSessionState.Session);

                try
                {
                    Process? process = Process.GetProcessById(processId);

                    await WriteOutputMessageAsync(string.Format(VSResources.ProjectHotReloadSessionManager_AttachingToProcess, _pendingSessionState.Session.Name, processId));

                    process.Exited += _pendingSessionState.OnProcessExited;
                    process.EnableRaisingEvents = true;

                    if (process.HasExited)
                    {
                        await WriteOutputMessageAsync(string.Format(VSResources.ProjectHotReloadSessionManager_ProcessAlreadyExited, _pendingSessionState.Session.Name));
                        process.Exited -= _pendingSessionState.OnProcessExited;
                        process = null;
                    }

                    _pendingSessionState.Process = process;
                }
                catch (Exception ex)
                {
                    await WriteOutputMessageAsync(string.Format(VSResources.ProjectHotReloadSessionManager_ErrorAttachingToProcess, _pendingSessionState.Session.Name, processId, ex.GetType(), ex.Message));
                }

                if (_pendingSessionState.Process is null)
                {
                    await WriteOutputMessageAsync(string.Format(VSResources.ProjectHotReloadSessionManager_NoActiveProcess, _pendingSessionState.Session.Name));
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
                && await GetDebugFrameworkVersionAsync() is string frameworkVersion)
            {
                if (await DebugFrameworkSupportsStartupHooksAsync())
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
                else
                {
                    // If startup hooks are not supported then tell the user why Hot Reload isn't available.
                    await WriteOutputMessageAsync(string.Format(VSResources.ProjectHotReloadSessionManager_StartupHooksDisabled, Path.GetFileNameWithoutExtension(_project.FullPath)));
                }
            }

            _pendingSessionState = null;
            return false;
        }

        private Task WriteOutputMessageAsync(string outputMessage) => _hotReloadDiagnosticOutputService.Value.WriteLineAsync(outputMessage);

        /// <summary>
        /// Checks if the project configuration targeted for debugging/launch meets the
        /// basic requirements to support Hot Reload. Note that there may be other, specific
        /// settings that prevent the use of Hot Reload even if the basic requirements are met.
        /// </summary>
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

            await WriteOutputMessageAsync(string.Format(VSResources.ProjectHotReloadSessionManager_ProcessExited, hotReloadState.Session.Name));

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
                await WriteOutputMessageAsync(string.Format(VSResources.ProjectHotReloadSessionManager_ErrorStoppingTheSession, hotReloadState.Session.Name, ex.GetType(), ex.Message));
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
