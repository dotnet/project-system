// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.Versioning;
using Microsoft.DotNet.HotReload;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

internal sealed class ProjectHotReloadSession : IProjectHotReloadSessionInternal
{
    public int Id { get; }
    public string Name { get; }

    private readonly Lazy<IHotReloadAgentManagerClient> _hotReloadAgentManagerClient;
    private readonly ConfiguredProject _configuredProject;
    private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadOutputService;
    private readonly IProjectHotReloadSessionCallback _callback;
    private readonly IProjectHotReloadBuildManager _buildManager;
    private readonly ILaunchProfile _launchProfile;
    private readonly DebugLaunchOptions _debugLaunchOptions;
    private readonly IProjectSystemOptions _projectSystemOptions;
    private readonly IHotReloadDebugStateProvider _debugStateProvider;
    private readonly IProjectHotReloadLaunchProvider _launchProvider;
    private readonly ReentrantSemaphore _semaphore;

    private bool _sessionActive;
    private IDeltaApplier? _lazyDeltaApplier;

    public ProjectHotReloadSession(
        string name,
        int id,
        Lazy<IHotReloadAgentManagerClient> hotReloadAgentManagerClient,
        Lazy<IHotReloadDiagnosticOutputService> hotReloadOutputService,
        IProjectHotReloadSessionCallback callback,
        IProjectHotReloadBuildManager buildManager,
        IProjectHotReloadLaunchProvider launchProvider,
        ConfiguredProject configuredProject,
        ILaunchProfile launchProfile,
        DebugLaunchOptions debugLaunchOptions,
        IProjectSystemOptions projectSystemOptions,
        IProjectThreadingService threadingService,
        IHotReloadDebugStateProvider debugStateProvider)
    {
        Name = name;
        Id = id;

        _configuredProject = configuredProject;
        _hotReloadAgentManagerClient = hotReloadAgentManagerClient;
        _hotReloadOutputService = hotReloadOutputService;
        _callback = callback;
        _launchProfile = launchProfile;
        _buildManager = buildManager;
        _launchProvider = launchProvider;
        _debugLaunchOptions = debugLaunchOptions;
        _projectSystemOptions = projectSystemOptions;
        _debugStateProvider = debugStateProvider;

        // We use a semaphore to prevent race conditions between StartSessionAsync and StopSessionAsync
        // where StopSessionAsync can be called before, during or after StartSessionAsync.
        _semaphore = ReentrantSemaphore.Create(
            initialCount: 1,
            joinableTaskContext: threadingService.JoinableTaskContext.Context,
            mode: ReentrantSemaphore.ReentrancyMode.Freeform);
    }

    /// <summary>
    /// Returns true and the path to the client agent implementation binary if the application needs the agent to be injected.
    /// </summary>
    private static string GetStartupHookPath(Version applicationTargetFrameworkVersion)
    {
        var hookTargetFramework = applicationTargetFrameworkVersion.Major >= 10 ? "net10.0" : "net6.0";
        return GetInjectedAssemblyPath(hookTargetFramework, "Microsoft.Extensions.DotNetDeltaApplier");
    }

    internal static string GetInjectedAssemblyPath(string targetFramework, string assemblyName)
        => Path.Combine(Path.GetDirectoryName(typeof(DeltaApplier).Assembly.Location)!, "HotReload", targetFramework, assemblyName + ".dll");

    public IDeltaApplier? DeltaApplier
        => _lazyDeltaApplier;

    public async Task ApplyChangesAsync(CancellationToken cancellationToken)
    {
        if (_sessionActive)
        {
            await _hotReloadAgentManagerClient.Value.ApplyUpdatesAsync(cancellationToken);
        }
    }

    private async ValueTask<IDeltaApplier> GetOrCreateDeltaApplierAsync(CancellationToken cancellationToken)
    {
        var applier = _lazyDeltaApplier;
        if (applier is not null)
        {
            return applier;
        }

        // The callback may provide a custom delta applier (e.g. MAUIDeltaApplier)
        applier = _callback.GetDeltaApplier();
        if (applier is null)
        {
            var targetFramework = await _configuredProject.GetProjectPropertyValueAsync(ConfigurationGeneral.TargetFrameworkProperty);
            var targetFrameworkMoniker = await _configuredProject.GetProjectPropertyValueAsync(ConfigurationGeneral.TargetFrameworkMonikerProperty);
            var targetFrameworkName = new FrameworkName(targetFrameworkMoniker);

            var loggerFactory = new HotReloadLoggerFactory(
                _hotReloadOutputService.Value,
                projectName: Name,
                targetFramework,
                sessionInstanceId: Id);

            var clientLogger = loggerFactory.CreateLogger("Project");
            var agentLogger = loggerFactory.CreateLogger("Agent");

            HotReloadClient client;
            if (_callback is IProjectHotReloadSessionWebAssemblyCallback wasmCallback)
            {
                var hotReloadCapabilitiesStr = await _configuredProject.GetProjectPropertyValueAsync("WebAssemblyHotReloadCapabilities");
                var hotReloadCapabilities = hotReloadCapabilitiesStr.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(static c => c.Trim()).ToImmutableArray();
                var browserRefreshServer = wasmCallback.BrowserRefreshServerAccessor.Server;

                client = new WebAssemblyHotReloadClient(clientLogger, agentLogger, browserRefreshServer, hotReloadCapabilities, targetFrameworkName.Version, suppressBrowserRequestsForTesting: false);
            }
            else
            {
                client = new DefaultHotReloadClient(clientLogger, agentLogger, GetStartupHookPath(targetFrameworkName.Version), enableStaticAssetUpdates: true);
            }

            applier = new DeltaApplier(client, _debugStateProvider);
        }

        if (applier is IDeltaApplierInternal applierInternal)
        {
            // Have to switch to background thread so that we don't block UI thread reading from the pipe:
            await TaskScheduler.Default;

            await applierInternal.InitiateConnectionAsync(cancellationToken);
        }

        _lazyDeltaApplier = applier;
        return applier;
    }

    /// <summary>
    /// Update environment of the process to be launched.
    /// </summary>
    public async Task<bool> ApplyLaunchVariablesAsync(IDictionary<string, string> envVars, CancellationToken cancellationToken)
    {
        var applier = await GetOrCreateDeltaApplierAsync(cancellationToken);
        return await applier.ApplyProcessEnvironmentVariablesAsync(envVars, cancellationToken);
    }

    // TODO: remove
    public Task StartSessionAsync(bool runningUnderDebugger, CancellationToken cancellationToken)
        => StartSessionAsync(cancellationToken);

    /// <summary>
    /// Starts the session right after the process has been started.
    /// </summary>
    public async Task StartSessionAsync(CancellationToken cancellationToken)
    {
        await _semaphore.ExecuteAsync(StartSessionInternalAsync, cancellationToken);
        async Task StartSessionInternalAsync()
        {
            if (_sessionActive)
            {
                throw new InvalidOperationException("Attempting to start a Hot Reload session that is already running.");
            }

            string targetFramework = await _configuredProject.GetProjectPropertyValueAsync(ConfigurationGeneral.TargetFrameworkProperty);
            bool hotReloadAutoRestart = await _configuredProject.GetProjectPropertyBoolAsync(ConfigurationGeneral.HotReloadAutoRestartProperty);

            var runningProjectInfo = new RunningProjectInfo
            {
                RestartAutomatically = hotReloadAutoRestart,
                ProjectInstanceId = new ProjectInstanceId
                {
                    ProjectFilePath = _configuredProject.UnconfiguredProject.FullPath,
                    TargetFramework = targetFramework,
                }
            };

            DebugTrace($"Start session for project '{_configuredProject.UnconfiguredProject.FullPath}' with TFM '{targetFramework}' and HotReloadRestart {runningProjectInfo.RestartAutomatically}");

            var processInfo = new ManagedEditAndContinueProcessInfo();

            // If the debugger uses ICorDebug, the debugger is responsible for applying deltas to the process and the Hot Reload client receives empty deltas.
            // Otherwise, the Hot Reload client is responsible for applying deltas and needs to receive them from the debugger.
            // This is controlled by IsDebuggedProcess flag.
            var flags = _debugLaunchOptions.HasFlag(DebugLaunchOptions.NoDebug) || await UsingLegacyWebAssemblyDebugEngineAsync(cancellationToken)
                ? HotReloadAgentFlags.None
                : HotReloadAgentFlags.IsDebuggedProcess;

            if (await GetOrCreateDeltaApplierAsync(cancellationToken) is IDeltaApplierInternal applierInternal)
            {
                await applierInternal.InitializeApplicationAsync(cancellationToken);
            }

            await _hotReloadAgentManagerClient.Value.AgentStartedAsync(this, flags, processInfo, runningProjectInfo, cancellationToken);

            WriteToOutputWindow(Resources.HotReloadStartSession, default);

            _sessionActive = true;
        }
    }

    private async ValueTask<bool> UsingLegacyWebAssemblyDebugEngineAsync(CancellationToken cancellationToken)
    {
        if (_callback is not IProjectHotReloadSessionWebAssemblyCallback)
        {
            // not a WASM app:
            return false;
        }

        if (await _projectSystemOptions.IsCorDebugWebAssemblyDebuggerEnabledAsync(cancellationToken) &&
            await IsCorDebugWebAssemblyDebuggerSupportedByProjectAsync())
        {
            // using new debugger:
            return false;
        }

        // using old debugger:
        return true;

        async ValueTask<bool> IsCorDebugWebAssemblyDebuggerSupportedByProjectAsync()
        {
            var targetFrameworkMoniker = await _configuredProject.GetProjectPropertyValueAsync(ConfigurationGeneral.TargetFrameworkMonikerProperty);
            return new FrameworkName(targetFrameworkMoniker).Version.Major >= 9;
        }
    }

    public async Task StopSessionAsync(CancellationToken cancellationToken)
    {
        await _semaphore.ExecuteAsync(StopSessionInternalAsync, cancellationToken);
        async Task StopSessionInternalAsync()
        {
            if (_sessionActive && _lazyDeltaApplier is not null)
            {
                _sessionActive = false;

                _lazyDeltaApplier.Dispose();
                _lazyDeltaApplier = null;

                await _hotReloadAgentManagerClient.Value.AgentTerminatedAsync(this, cancellationToken);
                WriteToOutputWindow(Resources.HotReloadStopSession, default);
            }
        }
    }

    public async ValueTask ApplyUpdatesAsync(ImmutableArray<ManagedHotReloadUpdate> updates, CancellationToken cancellationToken)
    {
        // A stricter check for session active could be done here, like raise an exception when not active session or no delta applier
        // But sometimes debugger would call ApplyUpdatesAsync even when there is not active session
        // e.g. when user restarts on rude edits
        // We need to talk to debugger team to see if we can avoid such calls in the future
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2581474
        if (_sessionActive is false || _lazyDeltaApplier is null)
        {
            DebugTrace($"{nameof(ApplyUpdatesAsync)} called but the session is not active.");
            return;
        }

        try
        {
            WriteToOutputWindow(Resources.HotReloadSendingUpdates, cancellationToken, HotReloadVerbosity.Detailed);

            ApplyResult result = await _lazyDeltaApplier.ApplyUpdatesAsync(updates, cancellationToken);

            if (result is ApplyResult.Success)
            {
                WriteToOutputWindow(Resources.HotReloadApplyUpdatesSuccessful, cancellationToken, HotReloadVerbosity.Detailed);

                if (_callback is not null)
                {
                    await _callback.OnAfterChangesAppliedAsync(cancellationToken);
                }
            }
        }
        catch (Exception e) when (LogAndPropagate(e, cancellationToken))
        {
            // unreachable
        }
    }

    private bool LogAndPropagate(Exception e, CancellationToken cancellationToken)
    {
        if (e is not OperationCanceledException)
        {
            WriteToOutputWindow(
                string.Format(Resources.HotReloadApplyUpdatesFailure, $"{e.GetType()}: {e.Message}"),
                cancellationToken,
                errorLevel: HotReloadDiagnosticErrorLevel.Error);
        }

        return false;
    }

    public async ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        // Delegate to the delta applier for the session
        if (_lazyDeltaApplier is not null)
        {
            return await _lazyDeltaApplier.GetCapabilitiesAsync(cancellationToken);
        }

        return [];
    }

    public ValueTask ReportDiagnosticsAsync(ImmutableArray<ManagedHotReloadDiagnostic> diagnostics, CancellationToken cancellationToken)
    {
        WriteToOutputWindow(Resources.HotReloadErrorsInApplication, cancellationToken, errorLevel: HotReloadDiagnosticErrorLevel.Error);

        foreach (ManagedHotReloadDiagnostic diagnostic in diagnostics)
        {
            WriteToOutputWindow(
                $"{diagnostic.FilePath}({diagnostic.Span.StartLine},{diagnostic.Span.StartColumn},{diagnostic.Span.EndLine},{diagnostic.Span.EndColumn}): {diagnostic.Message}",
                cancellationToken,
                errorLevel: HotReloadDiagnosticErrorLevel.Error);
        }

        return new ValueTask(Task.CompletedTask);
    }

    public async ValueTask RestartAsync(CancellationToken cancellationToken)
    {
        WriteToOutputWindow(Resources.HotReloadRestartInProgress, cancellationToken);
        if (_launchProfile.IsSkipStopAndRestartEnabled())
        {
            DebugTrace("Skipping Stop and Restart as per launch profile settings.");
            return;
        }

        await _launchProvider.LaunchWithProfileAsync(_debugLaunchOptions, _launchProfile, cancellationToken);
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        if (_launchProfile.IsSkipStopAndRestartEnabled())
        {
            DebugTrace("Skipping Stop as per launch profile settings.");
            return;
        }

        WriteToOutputWindow(Resources.HotReloadStoppingApplication, cancellationToken);

        await _callback.StopProjectAsync(cancellationToken);

        // TODO: Should we stop the session here? Or does someone else do it?
    }

    public ValueTask<bool> SupportsRestartAsync(CancellationToken cancellationToken)
        => new(true);

    private void WriteToOutputWindow(string message, CancellationToken cancellationToken, HotReloadVerbosity verbosity = HotReloadVerbosity.Minimal, HotReloadDiagnosticErrorLevel errorLevel = HotReloadDiagnosticErrorLevel.Info)
    {
        _hotReloadOutputService.Value.WriteLine(
            new HotReloadLogMessage(
                verbosity,
                message,
                Name,
                instanceId: (uint)Id,
                errorLevel: errorLevel),
            cancellationToken);
    }

    private void DebugTrace(string message)
    {
        _hotReloadOutputService.Value.WriteLine(
            new HotReloadLogMessage(
                HotReloadVerbosity.Detailed,
                message,
                Name,
                instanceId: (uint)Id,
                errorLevel: HotReloadDiagnosticErrorLevel.Info),
            CancellationToken.None);
    }

    public ValueTask<int?> GetTargetLocalProcessIdAsync(CancellationToken cancellationToken)
    {
        if (_callback is IProjectHotReloadSessionCallback2 callback2)
        {
            return new ValueTask<int?>(callback2.Process?.Id);
        }

        return new ValueTask<int?>();
    }

    public ValueTask<string?> GetProjectFullPathAsync(CancellationToken cancellationToken)
    {
        if (_callback is IProjectHotReloadSessionCallback2 callback2)
        {
            return new ValueTask<string?>(callback2.Project?.FullPath);
        }

        return new ValueTask<string?>();
    }

    public async ValueTask BuildAsync(CancellationToken cancellationToken)
    {
        var isBuildSucceed = await _buildManager.BuildProjectAsync(cancellationToken);

        if (!isBuildSucceed)
        {
            WriteToOutputWindow(Resources.HotReloadBuildFail, cancellationToken, HotReloadVerbosity.Minimal, HotReloadDiagnosticErrorLevel.Error);

            throw new InvalidOperationException();
        }
    }
}
