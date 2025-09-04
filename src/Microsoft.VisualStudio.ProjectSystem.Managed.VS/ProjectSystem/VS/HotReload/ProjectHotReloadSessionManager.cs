// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

[Export(typeof(IProjectHotReloadSessionManager))]
[Export(typeof(IProjectHotReloadUpdateApplier))]
internal sealed class ProjectHotReloadSessionManager : OnceInitializedOnceDisposedAsync, IProjectHotReloadSessionManager, IProjectHotReloadUpdateApplier
{
    private readonly ConfiguredProject _configuredProject;
    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly IProjectFaultHandlerService _projectFaultHandlerService;
    private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadDiagnosticOutputService;
    private readonly Lazy<IProjectHotReloadNotificationService> _projectHotReloadNotificationService;
    private readonly IProjectHotReloadAgent _hotReloadAgent;

    // Protect the state from concurrent access. For example, our Process.Exited event
    // handler may run on one thread while we're still setting up the session on
    // another. To ensure consistent and proper behavior we need to serialize access.
    private readonly ReentrantSemaphore _semaphore;

    private readonly Dictionary<int, HotReloadState> _activeSessions = [];
    private HotReloadState? _pendingSessionState = null;
    private int _nextUniqueId = 1;
    public bool HasActiveHotReloadSessions => _activeSessions.Count != 0;

    [ImportingConstructor]
    public ProjectHotReloadSessionManager(
        ConfiguredProject project,
        IProjectThreadingService threadingService,
        IProjectFaultHandlerService projectFaultHandlerService,
        Lazy<IHotReloadDiagnosticOutputService> hotReloadDiagnosticOutputService,
        Lazy<IProjectHotReloadNotificationService> projectHotReloadNotificationService,
        IProjectHotReloadLaunchProvider launchProvider,
        IProjectHotReloadBuildManager buildManager,
        IProjectHotReloadAgent hotReloadAgent)
        : base(threadingService.JoinableTaskContext)
    {
        _configuredProject = project;
        _unconfiguredProject = project.UnconfiguredProject;
        _projectFaultHandlerService = projectFaultHandlerService;
        _hotReloadDiagnosticOutputService = hotReloadDiagnosticOutputService;
        _projectHotReloadNotificationService = projectHotReloadNotificationService;
        _hotReloadAgent = hotReloadAgent;
        _semaphore = ReentrantSemaphore.Create(
            initialCount: 1,
            joinableTaskContext: threadingService.JoinableTaskContext.Context,
            mode: ReentrantSemaphore.ReentrancyMode.Freeform);
    }

    public async Task ActivateSessionAsync(int processId, string projectName)
    {
        await _semaphore.ExecuteAsync(ActivateSessionInternalAsync);

        async Task ActivateSessionInternalAsync()
        {
            if (_pendingSessionState is not null)
            {
                Assumes.NotNull(_pendingSessionState.Session);

                try
                {
                    Process? process = Process.GetProcessById(processId);

                    WriteOutputMessage(
                        new HotReloadLogMessage(
                            HotReloadVerbosity.Detailed,
                            Resources.ProjectHotReloadSessionManager_AttachingToProcess,
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
                                Resources.ProjectHotReloadSessionManager_ProcessAlreadyExited,
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
                            Resources.ProjectHotReloadSessionManager_NoActiveProcess,
                            projectName,
                            _pendingSessionState.Session.Name,
                            (uint)processId,
                            HotReloadDiagnosticErrorLevel.Warning
                        ),
                        default);
                }
                else
                {
                    await _pendingSessionState.Session.StartSessionAsync(cancellationToken: default);
                    _activeSessions.Add(processId, _pendingSessionState);

                    // Addition of the first session, puts the project in hot reload mode
                    if (_activeSessions.Count == 1)
                    {
                        await _projectHotReloadNotificationService.Value.SetHotReloadStateAsync(isInHotReload: true);
                    }
                }

                _pendingSessionState = null;
            }
        }
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
                    HotReloadState state = new(this, launchOptions);

                    IProjectHotReloadSession projectHotReloadSession = _hotReloadAgent.CreateHotReloadSession(
                        name: name,
                        id: _nextUniqueId++,
                        callback: state,
                        launchProfile: launchProfile,
                        configuredProject: _configuredProject,
                        debugLaunchOptions: launchOptions);

                    state.Session = projectHotReloadSession;
                    await projectHotReloadSession.ApplyLaunchVariablesAsync(environmentVariables, default);
                    _pendingSessionState = state;

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
            foreach (HotReloadState sessionState in _activeSessions.Values)
            {
                Assumes.NotNull(sessionState.Process);
                Assumes.NotNull(sessionState.Session);

                sessionState.Process.Exited -= sessionState.OnProcessExited;
                _projectFaultHandlerService.Forget(sessionState.Session.StopSessionAsync(default), _unconfiguredProject);
            }

            _activeSessions.Clear();

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

    private void OnProcessExited(HotReloadState hotReloadState)
    {
        _projectFaultHandlerService.Forget(OnProcessExitedAsync(hotReloadState), _unconfiguredProject);
    }

    private async Task OnProcessExitedAsync(HotReloadState hotReloadState)
    {
        Assumes.NotNull(hotReloadState.Session);
        Assumes.NotNull(hotReloadState.Process);

        WriteOutputMessage(
            new HotReloadLogMessage(
                HotReloadVerbosity.Minimal,
                Resources.ProjectHotReloadSessionManager_ProcessExited,
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

    private static Task OnAfterChangesAppliedAsync(HotReloadState hotReloadState, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private ValueTask<bool> StopProjectAsync(HotReloadState hotReloadState, CancellationToken cancellationToken)
    {
        return _semaphore.ExecuteAsync(StopProjectInternalAsync, cancellationToken);

        async ValueTask<bool> StopProjectInternalAsync()
        {
            Assumes.NotNull(hotReloadState.Session);
            Assumes.NotNull(hotReloadState.Process);

            int sessionCountOnEntry = _activeSessions.Count;

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
                        string.Format(Resources.ProjectHotReloadSessionManager_ErrorStoppingTheSession, ex.GetType(), ex.Message),
                        hotReloadState.Session?.Name,
                        null,
                        HotReloadDiagnosticOutputService.GetProcessId(hotReloadState.Process),
                        HotReloadDiagnosticErrorLevel.Error
                    ),
                    cancellationToken);
            }
            finally
            {
                // No more sessions removes the project from hot reload mode
                if (sessionCountOnEntry == 1 && _activeSessions.Count == 0)
                {
                    await _projectHotReloadNotificationService.Value.SetHotReloadStateAsync(isInHotReload: false);
                }
            }

            return true;
        }
    }

    /// <inheritdoc />
    public Task ApplyHotReloadUpdateAsync(Func<IDeltaApplier, CancellationToken, Task> applyFunction, CancellationToken cancellationToken)
    {
        return _semaphore.ExecuteAsync(ApplyHotReloadUpdateInternalAsync, cancellationToken);

        async Task ApplyHotReloadUpdateInternalAsync()
        {
            // Run the updates in parallel
            List<Task>? updateTasks = null;

            foreach (HotReloadState sessionState in _activeSessions.Values)
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

    private sealed class HotReloadState(
        ProjectHotReloadSessionManager sessionManager,
        DebugLaunchOptions launchOptions) : IProjectHotReloadSessionCallback2
    {
        public DebugLaunchOptions LaunchOptions { get; } = launchOptions;
        public UnconfiguredProject? Project => sessionManager._unconfiguredProject;

        public IProjectHotReloadSession? Session { get; set; }
        public Process? Process { get; set; }

        [Obsolete]
        public bool SupportsRestart => throw new NotImplementedException();

        internal void OnProcessExited(object sender, EventArgs e)
        {
            sessionManager.OnProcessExited(this);
        }

        public Task OnAfterChangesAppliedAsync(CancellationToken cancellationToken)
        {
            return ProjectHotReloadSessionManager.OnAfterChangesAppliedAsync(this, cancellationToken);
        }

        public Task<bool> StopProjectAsync(CancellationToken cancellationToken)
        {
            return sessionManager.StopProjectAsync(this, cancellationToken).AsTask();
        }

        public IDeltaApplier? GetDeltaApplier()
        {
            return ProjectHotReloadSessionManager.GetDeltaApplier(this);
        }

        [Obsolete]
        public Task<bool> RestartProjectAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
