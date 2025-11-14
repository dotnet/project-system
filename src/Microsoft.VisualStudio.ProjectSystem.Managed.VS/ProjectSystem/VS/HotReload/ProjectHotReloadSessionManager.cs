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
            await sessionState.Session.StopSessionAsync(sessionState.CancellationToken);

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

        Task DisposeCoreInternalAsync()
        {
            lock (_activeSessionStates)
            {
                foreach (HotReloadSessionState sessionState in _activeSessionStates)
                {
                    sessionState.Dispose();
                }

                _activeSessionStates.Clear();
            }

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
            HotReloadSessionState? sessionState = _pendingSessionState;

            // _pendingSessionState can be null if project doesn't support Hot Reload. i.e doesn't have SupportsHotReload capability
            if (sessionState is null)
            {
                return;
            }

            _pendingSessionState = null;

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
                sessionState.Dispose();
                return;
            }

            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) =>
            {
                DebugTrace("Process exited");
                sessionState.Dispose();
            };

            if (process.HasExited)
            {
                DebugTrace("Process exited");
                sessionState.Dispose();
            }
            else
            {
                try
                {
                    await sessionState.Session.StartSessionAsync(sessionState.CancellationToken);
                    await _projectHotReloadNotificationService.Value.SetHotReloadStateAsync(isInHotReload: true);
                }
                catch (OperationCanceledException)
                {
                    await sessionState.Session.StopSessionAsync(default);
                    sessionState.Dispose();
                }
            }
        }
    }

    private sealed class HotReloadSessionState : IProjectHotReloadSessionCallback, IDisposable
    {
        private int _disposed = 0;

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Action<HotReloadSessionState> _removeSessionState;
        private readonly IProjectThreadingService _threadingService;

        public HotReloadSessionState(
            Action<HotReloadSessionState> removeSessionState,
            IProjectThreadingService threadingService)
        {
            _removeSessionState = removeSessionState;
            _threadingService = threadingService;

            CancellationToken = _cancellationTokenSource.Token;
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
            if (DebuggerProcess is not null)
            {
                // need to call on UI thread
                await _threadingService.SwitchToUIThread(cancellationToken);

                // Ignore the debug option launching flags since we're just terminating the process, not the entire debug session
                DebuggerProcess.Terminate(ignoreLaunchFlags: 1);
            }
            else if (Process is not null)
            {
                // stop the process by killing it
                try
                {
                    if (!Process.HasExited)
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
            else
            {
                return true;
            }

            Dispose();
            await Session.StopSessionAsync(cancellationToken);

            return true;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            Process?.Dispose();

            _removeSessionState(this);
        }
    }

    private void DebugTrace(string message)
    {
#if DEBUG
        var projectName = _unconfiguredProject.GetProjectName();
        _hotReloadDiagnosticOutputService.Value.WriteLine(
            new HotReloadLogMessage(
                HotReloadVerbosity.Detailed,
                message,
                projectName,
                errorLevel: HotReloadDiagnosticErrorLevel.Info),
            CancellationToken.None);
#endif
    }
}
