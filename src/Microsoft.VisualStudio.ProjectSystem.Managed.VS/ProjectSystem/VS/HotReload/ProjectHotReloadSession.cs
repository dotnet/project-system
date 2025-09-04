// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

internal sealed class ProjectHotReloadSession : IProjectHotReloadSessionInternal
{
    public int Id { get; }
    public string Name { get; }

    private readonly Lazy<IHotReloadAgentManagerClient> _hotReloadAgentManagerClient;
    private readonly ConfiguredProject _configuredProject;
    private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadOutputService;
    private readonly Lazy<IManagedDeltaApplierCreator> _deltaApplierCreator;
    private readonly IProjectHotReloadSessionCallback _callback;
    private readonly IProjectHotReloadBuildManager _buildManager;
    private readonly ILaunchProfile _launchProfile;
    private readonly DebugLaunchOptions _debugLaunchOptions;
    private readonly IProjectHotReloadLaunchProvider _launchProvider;

    private bool _sessionActive;
    private IDeltaApplier? _deltaApplier;

    public ProjectHotReloadSession(
        string name,
        int id,
        Lazy<IHotReloadAgentManagerClient> hotReloadAgentManagerClient,
        Lazy<IHotReloadDiagnosticOutputService> hotReloadOutputService,
        Lazy<IManagedDeltaApplierCreator> deltaApplierCreator,
        IProjectHotReloadSessionCallback callback,
        IProjectHotReloadBuildManager buildManager,
        IProjectHotReloadLaunchProvider launchProvider,
        ConfiguredProject configuredProject,
        ILaunchProfile launchProfile,
        DebugLaunchOptions debugLaunchOptions)
    {
        Name = name;
        Id = id;

        _configuredProject = configuredProject;
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

    // TODO: remove
    public Task StartSessionAsync(bool runningUnderDebugger, CancellationToken cancellationToken)
        => StartSessionAsync(cancellationToken);

    /// <summary>
    /// Starts the session right after the process has been started.
    /// </summary>
    public async Task StartSessionAsync(CancellationToken cancellationToken)
    {
        if (_sessionActive)
        {
            throw new InvalidOperationException("Attempting to start a Hot Reload session that is already running.");
        }

        HotReloadAgentFlags flags = _debugLaunchOptions.HasFlag(DebugLaunchOptions.NoDebug) ? HotReloadAgentFlags.None : HotReloadAgentFlags.IsDebuggedProcess;

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

        DebugTrace($"start session for project '{_configuredProject.UnconfiguredProject.FullPath}' with TFM '{targetFramework}' and HotReloadRestart {runningProjectInfo.RestartAutomatically}");

        var processInfo = new ManagedEditAndContinueProcessInfo();
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

    [MemberNotNull(nameof(_deltaApplier))]
    private void EnsureDeltaApplierForSession()
    {
        _deltaApplier ??= _callback.GetDeltaApplier() ?? _deltaApplierCreator.Value.CreateManagedDeltaApplier(runtimeVersion: "0.0"); // the version is not used, just needs to parse

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
