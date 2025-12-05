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
                    HotReloadSessionState hotReloadSessionState = new(RemoveSessionState, _threadingService);

                    IProjectHotReloadSession projectHotReloadSession = _hotReloadAgent.CreateHotReloadSession(
                        name: projectName,
                        id: _nextUniqueId++,
                        callback: hotReloadSessionState,
                        launchProfile: launchProfile,
                        configuredProject: _configuredProject,
                        debugLaunchOptions: launchOptions);

                    hotReloadSessionState.Session = projectHotReloadSession;
                    await projectHotReloadSession.ApplyLaunchVariablesAsync(environmentVariables, hotReloadSessionState.CancellationToken);

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
                            0,
                            HotReloadDiagnosticErrorLevel.Warning
                        ));
                }
            }

            _pendingSessionState = null;
            return false;
        }
    }

    private void RemoveSessionState(HotReloadSessionState sessionState)
    {
        _threadingService.RunAndForget(async () =>
        {
            DebugTrace("Disposing Hot Reload session.");

            int count;
            lock (_activeSessionStates)
            {
                Assumes.True(_activeSessionStates.Remove(sessionState));
                count = _activeSessionStates.Count;
            }

            if (count == 0)
            {
                await _projectHotReloadNotificationService.Value.SetHotReloadStateAsync(isInHotReload: false);
            }
        }, _unconfiguredProject);
    }

    protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task DisposeCoreAsync(bool initialized)
    {
        return _semaphore.ExecuteAsync(DisposeCoreInternalAsync);

        async Task DisposeCoreInternalAsync()
        {
            List<Task> disposeTasks;
            lock (_activeSessionStates)
            {
                disposeTasks = _activeSessionStates.Select(session => session.DisposeAsync().AsTask()).ToList();
            }

            await Task.WhenAll(disposeTasks);
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

                if (sessionState.Session is IProjectHotReloadSessionInternal { DeltaApplier: { } deltaApplier })
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
            // _pendingSessionState can be null if project doesn't support Hot Reload. i.e doesn't have SupportsHotReload capability
            HotReloadSessionState? sessionState = Interlocked.Exchange(ref _pendingSessionState, null);

            if (sessionState is null)
            {
                DebugTrace("No pending session to start. Maybe the project doesn't support Hot Reload.");
                return;
            }

            DebugTrace("Hot Reload session started.");
            lock (_activeSessionStates)
            {
                _activeSessionStates.Add(sessionState);
            }

            Process? process = null;
            try
            {
                sessionState.DebuggerProcess = launchedProcess;
                process = Process.GetProcessById((int)vsDebugTargetProcessInfo.dwProcessId);
                sessionState.Process = process;
            }
            catch (ArgumentException)
            {
                // process might have been exited in some cases.
                // in that case, we early return without starting hotreload session
                // one way to mimic this is to hit control + C as fast as you can once hit F5/Control + F5
                await sessionState.DisposeAsync();
                return;
            }

            try
            {
                process.Exited += (sender, e) =>
                {
                    DebugTrace("Process exited");
                    _threadingService.ExecuteSynchronously(async () => await sessionState.DisposeAsync());
                };
                // If process exit before EnableRaisingEvents to true
                // An InvalidOperationException will be thrown
                process.EnableRaisingEvents = true;
                // At this stage, the process will be running, and it's exit event would be captured. by the exit handler
                // Because
                // - we register the exit event before starting the session
                // - we set EnableRaisingEvents to true, which performs as a safeguard against missing the exit event if the process exits quickly before we register the event.
                await sessionState.Session.StartSessionAsync(sessionState.CancellationToken);
                await _projectHotReloadNotificationService.Value.SetHotReloadStateAsync(isInHotReload: true);
            }
            catch (OperationCanceledException)
            {
                // This can happen if CancellationToken is cancelled while starting the session.
                await sessionState.DisposeAsync();
            }
            catch (InvalidOperationException)
            {
                // This can happen if we set EnableRaisingEvents to true after the process has already exited.
                await sessionState.DisposeAsync();
            }
        }
    }

    private sealed class HotReloadSessionState : IProjectHotReloadSessionCallback, IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Action<HotReloadSessionState> _removeSessionState;
        private readonly IProjectThreadingService _threadingService;
        private readonly ReentrantSemaphore _semaphore;

        private int _isClosed = 0;

        public HotReloadSessionState(
            Action<HotReloadSessionState> removeSessionState,
            IProjectThreadingService threadingService)
        {
            _removeSessionState = removeSessionState;
            _threadingService = threadingService;
            CancellationToken = _cancellationTokenSource.Token;

            _semaphore = ReentrantSemaphore.Create(
                initialCount: 1,
                joinableTaskContext: threadingService.JoinableTaskContext.Context,
                mode: ReentrantSemaphore.ReentrancyMode.NotAllowed);
        }

        public CancellationToken CancellationToken { get; }

        [Obsolete]
        public bool SupportsRestart => Session is not null;

        public IProjectHotReloadSession Session { get => field ?? throw Assumes.NotReachable(); set; }

        public Process? Process { get; set; }

        public IVsLaunchedProcess? DebuggerProcess { get; set; }

        public IDeltaApplier? GetDeltaApplier()
        {
            return null;
        }

        public Task OnAfterChangesAppliedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        [Obsolete]
        public Task<bool> RestartProjectAsync(CancellationToken cancellationToken)
        {
            return TaskResult.True;
        }

        public async Task<bool> StopProjectAsync(CancellationToken cancellationToken)
        {
            await CloseSessionAsync(stopProcess: true);

            return true;
        }

        public async ValueTask DisposeAsync()
        {
            await CloseSessionAsync(stopProcess: false);
        }

        private async Task CloseSessionAsync(bool stopProcess)
        {
            await _semaphore.ExecuteAsync(async () =>
            {
                if (Interlocked.Exchange(ref _isClosed, 1) == 1)
                {
                    // Ensure we only close the session once.
                    // Note that if multiple calls arrive with different stopProcess values, only the first will be honored.
                    // That is ok in the context of how the session is cleaned up today.
                    return;
                }

                // Disable the exit event handler from disposing the process during our explicit shutdown sequence.
                // We will handle that ourselves here once we're ready.
                Process?.EnableRaisingEvents = false;

                if (stopProcess)
                {
                    if (DebuggerProcess is not null && Process is not null)
                    {
                        // We have both DebuggerProcess and Process, they point to the same process. But DebuggerProcess provides a nicer way to terminate process
                        // without affecting the entire debug session.
                        // So we prefer to use DebuggerProcess to terminate the process first.

                        await TerminateProcessGracefullyAsync();

                        // When DebuggerProcess.Terminate(ignoreLaunchFlags: 1) return, the process might not be terminated
                        // So we first terminate the process nicely,
                        // Then wait for the process to exit. If the process doesn't exit within 500ms, kill it using traditional way.
                        await Process.WaitForExitAsync(default).WithTimeout(TimeSpan.FromMilliseconds(500));
                    }

                    if (Process is not null)
                    {
                        TerminateProcess(Process);
                    }
                }

                // Warning
                // Always cancel the CancellationTokenSource ahead of StopSessionAsync
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                Process?.Dispose();

                // In some occasions, StopSessionAsync might be invoked before StartSessionAsync
                // For example, if the process exits quickly after launch
                // So we call StopSessionAsync unconditionally to ensure the session is stopped properly
                await Session.StopSessionAsync(CancellationToken.None);

                _removeSessionState(this);

                return;

                async Task TerminateProcessGracefullyAsync()
                {
                    // Terminate DebuggerProcess need to call on UI thread
                    await _threadingService.SwitchToUIThread(CancellationToken.None);

                    // Ignore the debug option launching flags since we're just terminating the process, not the entire debug session
                    // TODO consider if we can use the return value of Terminate here to control whether we need to subsequently kill the process
                    DebuggerProcess.Terminate(ignoreLaunchFlags: 1);
                }

                static void TerminateProcess(Process process)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process has already exited.
                    }
                }
            });
        }
    }

    [Conditional("DEBUG")]
    private void DebugTrace(string message)
    {
        var projectName = _unconfiguredProject.GetProjectName();
        _hotReloadDiagnosticOutputService.Value.WriteLine(
            new HotReloadLogMessage(
                HotReloadVerbosity.Detailed,
                message,
                projectName,
                errorLevel: HotReloadDiagnosticErrorLevel.Info),
            CancellationToken.None);
    }
}
