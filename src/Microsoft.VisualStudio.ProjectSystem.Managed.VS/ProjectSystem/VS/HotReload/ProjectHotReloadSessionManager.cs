// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

[Export(typeof(IProjectHotReloadSessionManager))]
[Export(typeof(IProjectHotReloadUpdateApplier))]
[AppliesTo(ProjectCapability.SupportsHotReload)]
internal sealed class ProjectHotReloadSessionManager : OnceInitializedOnceDisposedAsync, IProjectHotReloadSessionManager, IProjectHotReloadUpdateApplier
{
    private readonly ConfiguredProject _configuredProject;
    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadDiagnosticOutputService;
    private readonly Lazy<IProjectHotReloadNotificationService> _projectHotReloadNotificationService;
    private readonly IProjectHotReloadAgent _hotReloadAgent;
    private readonly IProjectThreadingService _threadingService;

    // Protect the state from concurrent access. For example, our Process.Exited event
    // handler may run on one thread while we're still setting up the session on
    // another. To ensure consistent and proper behavior we need to serialize access.
    private readonly ReentrantSemaphore _semaphore;

    private readonly List<HotReloadSessionState> _activeSessionStates = [];
    private HotReloadSessionState? _pendingSessionState = null;
    private int _nextUniqueId = 1;
    public bool HasActiveHotReloadSessions => _activeSessionStates.Count != 0;

    [ImportingConstructor]
    public ProjectHotReloadSessionManager(
        ConfiguredProject project,
        IProjectThreadingService threadingService,
        Lazy<IHotReloadDiagnosticOutputService> hotReloadDiagnosticOutputService,
        Lazy<IProjectHotReloadNotificationService> projectHotReloadNotificationService,
        IProjectHotReloadLaunchProvider launchProvider,
        IProjectHotReloadBuildManager buildManager,
        IProjectHotReloadAgent hotReloadAgent)
        : base(threadingService.JoinableTaskContext)
    {
        _configuredProject = project;
        _unconfiguredProject = project.UnconfiguredProject;
        _threadingService = threadingService;
        _hotReloadDiagnosticOutputService = hotReloadDiagnosticOutputService;
        _projectHotReloadNotificationService = projectHotReloadNotificationService;
        _hotReloadAgent = hotReloadAgent;
        _semaphore = ReentrantSemaphore.Create(
            initialCount: 1,
            joinableTaskContext: threadingService.JoinableTaskContext.Context,
            mode: ReentrantSemaphore.ReentrancyMode.Freeform);
    }

    public Task<bool> TryCreatePendingSessionAsync(
        IProjectHotReloadLaunchProvider launchProvider,
        IDictionary<string, string> environmentVariables,
        DebugLaunchOptions launchOptions,
        ILaunchProfile launchProfile)
    {
        return _semaphore.ExecuteAsync(TryCreatePendingSessionInternalAsync).AsTask();

        async ValueTask<bool> TryCreatePendingSessionInternalAsync()
        {
            if (await ProjectSupportsHotReloadAsync())
            {
                var projectName = _unconfiguredProject.GetProjectName();

                if (await ProjectSupportsStartupHooksAsync())
                {
                    HotReloadSessionState hotReloadSessionState = new((HotReloadSessionState sessionState) =>
                    {
                        int count;
                        lock (_activeSessionStates)
                        {
                            Assumes.True(_activeSessionStates.Remove(sessionState), "Cannot remove unknown hot reload session.");
                            count = _activeSessionStates.Count;
                        }

                        if (count == 0)
                        {
                            _threadingService.ExecuteSynchronously(() => _projectHotReloadNotificationService.Value.SetHotReloadStateAsync(isInHotReload: false));
                        }
                    }, _threadingService);

                    IProjectHotReloadSession projectHotReloadSession = _hotReloadAgent.CreateHotReloadSession(
                        name: projectName,
                        id: _nextUniqueId++,
                        callback: hotReloadSessionState,
                        launchProfile: launchProfile,
                        configuredProject: _configuredProject,
                        debugLaunchOptions: launchOptions);

                    hotReloadSessionState.Session = projectHotReloadSession;
                    await projectHotReloadSession.ApplyLaunchVariablesAsync(environmentVariables, default);

                    _pendingSessionState = hotReloadSessionState;

                    return true;
                }
                else
                {
                    // If startup hooks are not supported then tell the user why Hot Reload isn't available.

                    WriteOutputMessage(
                        new HotReloadLogMessage(
                            HotReloadVerbosity.Minimal,
                            Resources.ProjectHotReloadSessionManager_StartupHooksDisabled,
                            projectName,
                            null,
                            HotReloadDiagnosticOutputService.GetProcessId(),
                            HotReloadDiagnosticErrorLevel.Warning
                        ));
                }
            }

            _pendingSessionState = null;
            return false;
        }
    }

    protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task DisposeCoreAsync(bool initialized)
    {
        return _semaphore.ExecuteAsync(DisposeCoreInternalAsync);

        Task DisposeCoreInternalAsync()
        {
            foreach (HotReloadSessionState sessionState in _activeSessionStates)
            {
                sessionState.Process?.Dispose();
            }

            _activeSessionStates.Clear();

            return Task.CompletedTask;
        }
    }

    private void WriteOutputMessage(HotReloadLogMessage hotReloadLogMessage, CancellationToken cancellationToken = default) => _hotReloadDiagnosticOutputService.Value.WriteLine(hotReloadLogMessage, cancellationToken);

    /// <summary>
    /// Checks if the project meets the basic requirements to support Hot Reload. Note that there may be other, specific
    /// settings that prevent the use of Hot Reload even if the basic requirements are met.
    /// </summary>
    private async ValueTask<bool> ProjectSupportsHotReloadAsync()
        => _configuredProject.Capabilities.AppliesTo(ProjectCapability.SupportsHotReload) &&
           await _configuredProject.GetProjectPropertyBoolAsync(ConfiguredBrowseObject.DebugSymbolsProperty) &&
           !await _configuredProject.GetProjectPropertyBoolAsync(ConfigurationGeneral.OptimizeProperty) &&
           await _configuredProject.GetProjectPropertyValueAsync(ConfigurationGeneral.TargetFrameworkProperty) is not "";

    /// <summary>
    /// Returns whether or not the project supports startup hooks. These are used to start Hot Reload in the launched process.
    /// </summary>
    private async ValueTask<bool> ProjectSupportsStartupHooksAsync()
        => await _configuredProject.GetProjectPropertyBoolAsync(ConfigurationGeneral.StartupHookSupportProperty, defaultValue: true);

    /// <inheritdoc />
    public Task ApplyHotReloadUpdateAsync(Func<IDeltaApplier, CancellationToken, Task> applyFunction, CancellationToken cancellationToken)
    {
        return _semaphore.ExecuteAsync(ApplyHotReloadUpdateInternalAsync, cancellationToken);

        async Task ApplyHotReloadUpdateInternalAsync()
        {
            // Run the updates in parallel
            List<Task>? updateTasks = null;

            foreach (HotReloadSessionState sessionState in _activeSessionStates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (sessionState.Session is IProjectHotReloadSessionInternal { DeltaApplier: {} deltaApplier })
                {
                    updateTasks ??= [];
                    updateTasks.Add(applyFunction(deltaApplier, cancellationToken));
                }
            }

            // Wait for their completion
            if (updateTasks is not null)
            {
                await Task.WhenAll(updateTasks);
            }
        }
    }

    public Task ActivateSessionAsync(IVsLaunchedProcess? launchedProcess, VsDebugTargetProcessInfo vsDebugTargetProcessInfo)
    {
        return _semaphore.ExecuteAsync(ActivateSessionInternalAsync);

        async Task ActivateSessionInternalAsync()
        {
            if (_pendingSessionState is { Session: IProjectHotReloadSession session })
            {
                Process process = Process.GetProcessById((int)vsDebugTargetProcessInfo.dwProcessId);
                _pendingSessionState.LaunchedProcess = launchedProcess;
                _pendingSessionState.Process = process;

                if (!process.HasExited)
                {
                    process.Exited += (sender, e) =>
                    {
                        _threadingService.ExecuteSynchronously(() => session.StopSessionAsync(CancellationToken.None));
                    };
                    process.EnableRaisingEvents = true;

                    await _pendingSessionState.Session.StartSessionAsync(CancellationToken.None);
                    lock (_activeSessionStates)
                    {
                        _activeSessionStates.Add(_pendingSessionState);
                    }
                    await _projectHotReloadNotificationService.Value.SetHotReloadStateAsync(isInHotReload: true);
                }

                _pendingSessionState = null;
            }
        }
    }

    private sealed class HotReloadSessionState(
        Action<HotReloadSessionState> removeSessionState,
        IProjectThreadingService threadingService) : IProjectHotReloadSessionCallback
    {
        public bool SupportsRestart => Session is not null;

        public IProjectHotReloadSession? Session { get; set; }

        public Process? Process { get; set; }

        public IVsLaunchedProcess? LaunchedProcess { get; set; }

        public IDeltaApplier? GetDeltaApplier()
        {
            return null;
        }

        public Task OnAfterChangesAppliedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<bool> RestartProjectAsync(CancellationToken cancellationToken)
        {
            return TaskResult.True;
        }

        public async Task<bool> StopProjectAsync(CancellationToken cancellationToken)
        {
            if(Session is null || LaunchedProcess is null)
            {
                return true;
            }

            // prefer to terminate launched process first if we have it
            if (LaunchedProcess is not null)
            {
                // need to call on UI thread
                await threadingService.SwitchToUIThread(cancellationToken);

                // Ignore the debug option launching flags since we're just terminating the process, not the entire debug session
                LaunchedProcess.Terminate(ignoreLaunchFlags: 1);
            }
            else
            {
                // stop the process by killing it
                try
                {
                    if (Process is not null && !Process.HasExited)
                    {
                        // First try to close the process nicely and if that doesn't work kill it.
                        if (!Process.CloseMainWindow())
                        {
                            Process.Kill();
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process has already exited.
                }
            }

            if (Session is not null)
            {
                await Session.StopSessionAsync(cancellationToken);
                removeSessionState(this);

                Session = null;
            }

            return true;
        }
    }
}
