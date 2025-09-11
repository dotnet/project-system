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

    private readonly List<HotReloadSessionCallback> _activeSessionState = [];
    private HotReloadSessionCallback? _pendingSession = null;
    private int _nextUniqueId = 1;
    public bool HasActiveHotReloadSessions => _activeSessionState.Count != 0;

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

    [Obsolete("Use ActivateSessionAsync overload that takes IVsLaunchedProcess and VsDebugTargetProcessInfo")]
    public Task ActivateSessionAsync(int processId, string projectName)
    {
        throw new InvalidOperationException("This overload of ActivateSessionAsync should not be called. Use another ActiveSessionAsync instead.");
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
                if (await ProjectSupportsStartupHooksAsync())
                {
                    string name = Path.GetFileNameWithoutExtension(_unconfiguredProject.FullPath);
                    HotReloadSessionCallback state = new((HotReloadSessionCallback session) =>
                    {
                        lock (_activeSessionState)
                        {
                            _activeSessionState.Remove(session);

                            if (_activeSessionState.Count == 0)
                            {
                                _threadingService.ExecuteSynchronously(() => _projectHotReloadNotificationService.Value.SetHotReloadStateAsync(false));
                            }
                        }
                    }, _threadingService);

                    IProjectHotReloadSession projectHotReloadSession = _hotReloadAgent.CreateHotReloadSession(
                        name: name,
                        id: _nextUniqueId++,
                        callback: state,
                        launchProfile: launchProfile,
                        configuredProject: _configuredProject,
                        debugLaunchOptions: launchOptions);

                    state.Session = projectHotReloadSession;
                    await projectHotReloadSession.ApplyLaunchVariablesAsync(environmentVariables, default);

                    _pendingSession = state;

                    return true;
                }
                else
                {
                    // If startup hooks are not supported then tell the user why Hot Reload isn't available.
                    string projectName = Path.GetFileNameWithoutExtension(_unconfiguredProject.FullPath);

                    WriteOutputMessage(
                        new HotReloadLogMessage(
                            HotReloadVerbosity.Minimal,
                            Resources.ProjectHotReloadSessionManager_StartupHooksDisabled,
                            projectName,
                            null,
                            HotReloadDiagnosticOutputService.GetProcessId(),
                            HotReloadDiagnosticErrorLevel.Warning
                        ),
                        default);
                }
            }

            _pendingSession = null;
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
            foreach (HotReloadSessionCallback sessionState in _activeSessionState)
            {
                sessionState.Process?.Dispose();
            }

            _activeSessionState.Clear();

            return Task.CompletedTask;
        }
    }

    private void WriteOutputMessage(HotReloadLogMessage hotReloadLogMessage, CancellationToken cancellationToken) => _hotReloadDiagnosticOutputService.Value.WriteLine(hotReloadLogMessage, cancellationToken);

    /// <summary>
    /// Checks if the project meets the basic requirements to support Hot Reload. Note that there may be other, specific
    /// settings that prevent the use of Hot Reload even if the basic requirements are met.
    /// </summary>
    private async ValueTask<bool> ProjectSupportsHotReloadAsync()
        => _configuredProject.Capabilities.AppliesTo("SupportsHotReload") &&
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

            foreach (HotReloadSessionCallback sessionState in _activeSessionState)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (sessionState.Session is IProjectHotReloadSessionInternal sessionInternal)
                {
                    IDeltaApplier? deltaApplier = sessionInternal.DeltaApplier;
                    if (deltaApplier is not null)
                    {
                        updateTasks ??= [];
                        updateTasks.Add(applyFunction(deltaApplier, cancellationToken));
                    }
                }
            }

            // Wait for their completion
            if (updateTasks is not null)
            {
                await Task.WhenAll(updateTasks);
            }
        }
    }

    public async Task ActivateSessionAsync(IVsLaunchedProcess launchedProcess, VsDebugTargetProcessInfo vsDebugTargetProcessInfo)
    {
        await _semaphore.ExecuteAsync(ActiveSessionInternalAsync);

        async Task ActiveSessionInternalAsync()
        {
            if (_pendingSession is not null && _pendingSession.Session is IProjectHotReloadSession session)
            {
                Process process = Process.GetProcessById((int)vsDebugTargetProcessInfo.dwProcessId);
                _pendingSession.LaunchedProcess = launchedProcess;
                _pendingSession.Process = process;

                if (!process.HasExited)
                {
                    process.Exited += (sender, e) =>
                    {
                        _threadingService.ExecuteSynchronously(() => session.StopSessionAsync(CancellationToken.None));
                    };
                    process.EnableRaisingEvents = true;

                    await _pendingSession.Session.StartSessionAsync(CancellationToken.None);
                    lock (_activeSessionState)
                    {
                        _activeSessionState.Add(_pendingSession);
                    }
                    await _projectHotReloadNotificationService.Value.SetHotReloadStateAsync(isInHotReload: true);
                }

                _pendingSession = null;
            }
        }
    }

    private sealed class HotReloadSessionCallback(
        Action<HotReloadSessionCallback> removeSessionState,
        IProjectThreadingService threadingService) : IProjectHotReloadSessionCallback
    {
        private static readonly Task<bool> s_trueTask = Task.FromResult(true);
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
            return s_trueTask;
        }

        public async Task<bool> StopProjectAsync(CancellationToken cancellationToken)
        {
            if( Session is null || LaunchedProcess is null)
            {
                return true;
            }

            // need to call on UI thread
            await threadingService.SwitchToUIThread(cancellationToken);

            LaunchedProcess.Terminate(1);

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
