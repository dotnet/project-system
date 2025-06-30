// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

internal sealed class ProjectHotReloadSession : IManagedHotReloadAgent, IManagedHotReloadAgent2, IManagedHotReloadAgent4, IProjectHotReloadSession, IProjectHotReloadSessionInternal
{
    private readonly string _variant;
    private readonly string _runtimeVersion;
    private readonly Lazy<IHotReloadAgentManagerClient> _hotReloadAgentManagerClient;
    private readonly ConfiguredProject? _configuredProject;
    private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadOutputService;
    private readonly Lazy<IManagedDeltaApplierCreator> _deltaApplierCreator;
    private readonly IProjectHotReloadSessionCallback _callback;
    private readonly IProjectHotReloadBuildManager? _buildManager;
    private readonly ILaunchProfile? _launchProfile;
    private readonly DebugLaunchOptions? _debugLaunchOptions;
    private readonly IProjectHotReloadLaunchProvider? _launchProvider;
    private bool _sessionActive;
    private IDeltaApplier? _deltaApplier;

    public ProjectHotReloadSession(
        string name,
        int variant,
        string runtimeVersion,
        Lazy<IHotReloadAgentManagerClient> hotReloadAgentManagerClient,
        Lazy<IHotReloadDiagnosticOutputService> hotReloadOutputService,
        Lazy<IManagedDeltaApplierCreator> deltaApplierCreator,
        IProjectHotReloadSessionCallback callback,
        IProjectHotReloadBuildManager? buildManager = null,
        IProjectHotReloadLaunchProvider? launchProvider = null,
        ConfiguredProject? configuredProject = null,
        ILaunchProfile? launchProfile = null,
        DebugLaunchOptions? debugLaunchOptions = null)
    {
        _configuredProject = configuredProject;
        Name = name;
        _variant = variant.ToString();
        _runtimeVersion = runtimeVersion;
        _hotReloadAgentManagerClient = hotReloadAgentManagerClient;
        _hotReloadOutputService = hotReloadOutputService;
        _deltaApplierCreator = deltaApplierCreator;
        _callback = callback;
        _launchProfile = launchProfile;
        _buildManager = buildManager;
        _launchProvider = launchProvider;
        _debugLaunchOptions = debugLaunchOptions;
    }

    // IProjectHotReloadSession

    public string Name { get; }

    public IDeltaApplier? DeltaApplier => _deltaApplier;

    public async Task ApplyChangesAsync(CancellationToken cancellationToken)
    {
        if (_sessionActive)
        {
            await _hotReloadAgentManagerClient.Value.ApplyUpdatesAsync(cancellationToken);
        }
    }

    public async Task<bool> ApplyLaunchVariablesAsync(IDictionary<string, string> envVars, CancellationToken cancellationToken)
    {
        EnsureDeltaApplierForSession();

        return await _deltaApplier.ApplyProcessEnvironmentVariablesAsync(envVars, cancellationToken);
    }

    public Task StartSessionAsync(CancellationToken cancellationToken)
    {
        return StartSessionAsync(runningUnderDebugger: false, cancellationToken);
    }

    public async Task StartSessionAsync(bool runningUnderDebugger, CancellationToken cancellationToken)
    {
        if (_sessionActive)
        {
            throw new InvalidOperationException("Attempting to start a Hot Reload session that is already running.");
        }

        HotReloadAgentFlags flags = runningUnderDebugger ? HotReloadAgentFlags.IsDebuggedProcess : HotReloadAgentFlags.None;
        var processInfo = new ManagedEditAndContinueProcessInfo();
        string targetFramework = _configuredProject?.Services.ProjectPropertiesProvider?.GetCommonProperties() is IProjectProperties commonProperties
            ? await commonProperties.GetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetFrameworkProperty)
            : string.Empty;

        RunningProjectInfo runningProjectInfo = new RunningProjectInfo
        {
            RestartAutomatically = false,
            ProjectInstanceId = new ProjectInstanceId
            {
                ProjectFilePath = _configuredProject?.UnconfiguredProject.FullPath ?? string.Empty,
                TargetFramework = targetFramework,
            }
        };

        DebugTrace($"start session for project '{_configuredProject?.UnconfiguredProject.FullPath}' with TFM '{runningProjectInfo.ProjectInstanceId.TargetFramework}' and HotReloadRestart {runningProjectInfo.RestartAutomatically}");

        await _hotReloadAgentManagerClient.Value.AgentStartedAsync(this, flags, processInfo, runningProjectInfo, cancellationToken);

        WriteToOutputWindow(Resources.HotReloadStartSession, default);
        _sessionActive = true;
        EnsureDeltaApplierForSession();
    }

    public async Task StopSessionAsync(CancellationToken cancellationToken)
    {
        if (_sessionActive)
        {
            _sessionActive = false;
            await _hotReloadAgentManagerClient.Value.AgentTerminatedAsync(this, cancellationToken);

            WriteToOutputWindow(Resources.HotReloadStopSession, default);
        }
    }

    // IManagedHotReloadAgent

    public async ValueTask ApplyUpdatesAsync(ImmutableArray<ManagedHotReloadUpdate> updates, CancellationToken cancellationToken)
    {
        if (!_sessionActive)
        {
            DebugTrace($"{nameof(ApplyUpdatesAsync)} called but the session is not active.");
            return;
        }

        if (_deltaApplier is null)
        {
            DebugTrace($"{nameof(ApplyUpdatesAsync)} called but we have no delta applier.");
            return;
        }

        try
        {
            WriteToOutputWindow(Resources.HotReloadSendingUpdates, cancellationToken, HotReloadVerbosity.Detailed);

            ApplyResult result = await _deltaApplier.ApplyUpdatesAsync(updates, cancellationToken);

            if (result is ApplyResult.Success or ApplyResult.SuccessRefreshUI)
            {
                WriteToOutputWindow(Resources.HotReloadApplyUpdatesSuccessful, cancellationToken, HotReloadVerbosity.Detailed);

                if (_callback is not null)
                {
                    await _callback.OnAfterChangesAppliedAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            WriteToOutputWindow(
                string.Format(Resources.HotReloadApplyUpdatesFailure, $"{ex.GetType()}: {ex.Message}"),
                cancellationToken,
                errorLevel: HotReloadDiagnosticErrorLevel.Error);
            throw;
        }
    }

    public async ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        // Delegate to the delta applier for the session
        if (_deltaApplier is not null)
        {
            return await _deltaApplier.GetCapabilitiesAsync(cancellationToken);
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
        if (_launchProfile is not null && _debugLaunchOptions.HasValue && _launchProvider is not null && _buildManager is not null)
        {
            // build project first
            var isSucceed = await _buildManager.BuildProjectAsync(cancellationToken);

            if (!isSucceed)
            {
                WriteToOutputWindow(Resources.HotReloadBuildFail, cancellationToken, HotReloadVerbosity.Minimal, HotReloadDiagnosticErrorLevel.Error);
                return;
            }

            await _launchProvider.LaunchWithProfileAsync(_debugLaunchOptions.Value, _launchProfile, cancellationToken);
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        WriteToOutputWindow(Resources.HotReloadStoppingApplication, cancellationToken);

        await _callback.StopProjectAsync(cancellationToken);

        // TODO: Should we stop the session here? Or does someone else do it?
    }

    public ValueTask<bool> SupportsRestartAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<bool>(_launchProfile is not null && _debugLaunchOptions.HasValue && _launchProvider is not null);
    }

    private void WriteToOutputWindow(string message, CancellationToken cancellationToken, HotReloadVerbosity verbosity = HotReloadVerbosity.Minimal, HotReloadDiagnosticErrorLevel errorLevel = HotReloadDiagnosticErrorLevel.Info)
    {
        _hotReloadOutputService.Value.WriteLine(
            new HotReloadLogMessage(
                verbosity,
                message,
                Name,
                _variant,
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
                _variant,
                errorLevel: HotReloadDiagnosticErrorLevel.Info),
            CancellationToken.None);
    }

    [MemberNotNull(nameof(_deltaApplier))]
    private void EnsureDeltaApplierForSession()
    {
        _deltaApplier ??= _callback.GetDeltaApplier() ?? _deltaApplierCreator.Value.CreateManagedDeltaApplier(_runtimeVersion);

        Assumes.NotNull(_deltaApplier);
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
}
