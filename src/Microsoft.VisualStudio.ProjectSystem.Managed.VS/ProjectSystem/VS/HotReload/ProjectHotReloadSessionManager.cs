// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

[Export(typeof(IProjectHotReloadSessionManager))]
[Export(typeof(IProjectHotReloadUpdateApplier))]
internal class ProjectHotReloadSessionManager : OnceInitializedOnceDisposedAsync, IProjectHotReloadSessionManager, IProjectHotReloadUpdateApplier
{
    private readonly UnconfiguredProject _project;
    private readonly IProjectFaultHandlerService _projectFaultHandlerService;
    private readonly IActiveDebugFrameworkServices _activeDebugFrameworkServices;
    private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadDiagnosticOutputService;
    private readonly Lazy<IProjectHotReloadNotificationService> _projectHotReloadNotificationService;
    private readonly Lazy<IManagedDeltaApplierCreator> _managedDeltaApplierCreator;
    private readonly Lazy<IHotReloadAgentManagerClient> _hotReloadAgentManagerClient;

    private readonly IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> _vsSolutionBuildManagerService;
    private IVsSolutionBuildManager2? _vsSolutionBuildManager2;
    private readonly IProjectThreadingService _projectThreadingService;

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
        UnconfiguredProject project,
        IProjectThreadingService threadingService,
        IProjectFaultHandlerService projectFaultHandlerService,
        IActiveDebugFrameworkServices activeDebugFrameworkServices,
        Lazy<IHotReloadDiagnosticOutputService> hotReloadDiagnosticOutputService,
        Lazy<IProjectHotReloadNotificationService> projectHotReloadNotificationService,
        Lazy<IManagedDeltaApplierCreator> managedDeltaApplierCreator,
        Lazy<IHotReloadAgentManagerClient> hotReloadAgentManagerClient,
        IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> solutionBuildManagerService)
        : base(threadingService.JoinableTaskContext)
    {
        _project = project;
        _projectThreadingService = threadingService;
        _projectFaultHandlerService = projectFaultHandlerService;
        _activeDebugFrameworkServices = activeDebugFrameworkServices;
        _hotReloadDiagnosticOutputService = hotReloadDiagnosticOutputService;
        _projectHotReloadNotificationService = projectHotReloadNotificationService;
        _vsSolutionBuildManagerService = solutionBuildManagerService;
        _managedDeltaApplierCreator = managedDeltaApplierCreator;
        _hotReloadAgentManagerClient = hotReloadAgentManagerClient;
        _semaphore = ReentrantSemaphore.Create(
            initialCount: 1,
            joinableTaskContext: project.Services.ThreadingPolicy.JoinableTaskContext.Context,
            mode: ReentrantSemaphore.ReentrancyMode.Freeform);
    }

    public async Task ActivateSessionAsync(int processId, bool runningUnderDebugger, string projectName)
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

    public Task<bool> TryCreatePendingSessionAsync(IDictionary<string, string> environmentVariables, DebugLaunchOptions? launchOptions = null, ILaunchProfile? launchProfile = null)
    {
        return _semaphore.ExecuteAsync(TryCreatePendingSessionInternalAsync).AsTask();

        async ValueTask<bool> TryCreatePendingSessionInternalAsync()
        {
            if (await DebugFrameworkSupportsHotReloadAsync()
                && await GetDebugFrameworkVersionAsync() is string frameworkVersion
                && !string.IsNullOrWhiteSpace(frameworkVersion))
            {
                if (await DebugFrameworkSupportsStartupHooksAsync())
                {
                    string name = Path.GetFileNameWithoutExtension(_project.FullPath);
                    HotReloadState state = new(this);
                    var configuredProject = await GetConfiguredProjectForDebugAsync();
                    Assumes.Present(configuredProject);

                    var debugLaunchProvider = configuredProject.Services.ExportProvider.GetExportedValueOrDefault<IInternalDebugLaunchProvider>();
                    IProjectHotReloadSession projectHotReloadSession = new ProjectHotReloadSession(
                        name: name,
                        variant: _nextUniqueId++,
                        runtimeVersion: frameworkVersion,
                        hotReloadAgentManagerClient: _hotReloadAgentManagerClient,
                        hotReloadOutputService: _hotReloadDiagnosticOutputService,
                        deltaApplierCreator: _managedDeltaApplierCreator,
                        sessionManager: this,
                        callback: state,
                        launchProvider: debugLaunchProvider,
                        launchProfile: launchProfile,
                        debugLaunchOptions: launchOptions);

                    state.Session = projectHotReloadSession;
                    await projectHotReloadSession.ApplyLaunchVariablesAsync(environmentVariables, default);
                    _pendingSessionState = state;

                    return true;
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
                _projectFaultHandlerService.Forget(sessionState.Session.StopSessionAsync(default), _project);
            }

            _activeSessions.Clear();

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Build project and wait for the build to complete.
    /// </summary>
    public async Task<bool> BuildProjectAsync(CancellationToken cancellationToken)
    {
        Assumes.NotNull(_project.Services.HostObject);
        _vsSolutionBuildManager2 ??= await _vsSolutionBuildManagerService.GetValueAsync(cancellationToken);

        if (_projectThreadingService.JoinableTaskContext.IsMainThreadBlocked())
        {
            throw new InvalidOperationException("This task cannot be blocked on by the UI thread.");
        }

        // Step 1: Register sbm events
        using var solutionBuildCompleteListener = new SolutionBuildCompleteListener();
        Verify.HResult(_vsSolutionBuildManager2.AdviseUpdateSolutionEvents(solutionBuildCompleteListener, out uint cookie));
        try
        {
            // Step 2: Build
            var projectVsHierarchy = (IVsHierarchy)_project.Services.HostObject;

            var result = _vsSolutionBuildManager2.StartSimpleUpdateProjectConfiguration(
                pIVsHierarchyToBuild: projectVsHierarchy,
                pIVsHierarchyDependent: null,
                pszDependentConfigurationCanonicalName: null,
                dwFlags: (uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD,
                dwDefQueryResults: (uint)VSSOLNBUILDQUERYRESULTS.VSSBQR_SAVEBEFOREBUILD_QUERY_YES,
                fSuppressUI: 0);

            ErrorHandler.ThrowOnFailure(result);

            // Step 3: Wait for the build to complete
            return await solutionBuildCompleteListener.WaitForSolutionBuildCompletedAsync(cancellationToken);
        }
        finally
        {
            _vsSolutionBuildManager2.UnadviseUpdateSolutionEvents(cookie);
        }
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

    private Task<ConfiguredProject?> GetConfiguredProjectForDebugAsync()
        => _activeDebugFrameworkServices.GetConfiguredProjectForActiveFrameworkAsync();

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
                        string.Format(VSResources.ProjectHotReloadSessionManager_ErrorStoppingTheSession, ex.GetType(), ex.Message),
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

    private class HotReloadState : IProjectHotReloadSessionCallback2
    {
        private readonly ProjectHotReloadSessionManager _sessionManager;

        public Process? Process { get; set; }
        public IProjectHotReloadSession? Session { get; set; }

        public UnconfiguredProject? Project => _sessionManager._project;

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
            return _sessionManager.StopProjectAsync(this, cancellationToken).AsTask();
        }

        public IDeltaApplier? GetDeltaApplier()
        {
            return ProjectHotReloadSessionManager.GetDeltaApplier(this);
        }
    }

    private class SolutionBuildCompleteListener : IVsUpdateSolutionEvents, IDisposable
    {
        private readonly TaskCompletionSource<bool> _buildCompletedSource = new();

        public SolutionBuildCompleteListener()
        {
        }

        public static int UpdateSolution_Start(ref int pfCancelUpdate)
        {
            return HResult.OK;
        }
        public static int UpdateSolution_Progress(ref int pfCancelUpdate)
        {
            return HResult.OK;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return HResult.OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            _buildCompletedSource.TrySetResult(fSucceeded != 0);

            return HResult.OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return HResult.OK;
        }

        public int UpdateSolution_Cancel()
        {
            _buildCompletedSource.TrySetCanceled();

            return HResult.OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return HResult.OK;
        }

        public async Task<bool> WaitForSolutionBuildCompletedAsync(CancellationToken ct = default)
        {
            using var _ = ct.Register(() =>
            {
                _buildCompletedSource.TrySetCanceled();
            });

            try
            {
                return await _buildCompletedSource.Task;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            _buildCompletedSource.TrySetCanceled();
        }
    }
}
