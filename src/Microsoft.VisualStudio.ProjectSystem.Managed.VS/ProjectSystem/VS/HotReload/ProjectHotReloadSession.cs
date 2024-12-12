// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

internal class ProjectHotReloadSession : IManagedHotReloadAgent, IManagedHotReloadAgent2, IManagedHotReloadAgent4, IProjectHotReloadSession, IProjectHotReloadSessionInternal
{
    private readonly string _variant;
    private readonly string _runtimeVersion;
    private readonly Lazy<IHotReloadAgentManagerClient> _hotReloadAgentManagerClient;
    private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadOutputService;
    private readonly Lazy<IManagedDeltaApplierCreator> _deltaApplierCreator;
    private readonly IProjectHotReloadSessionCallback _callback;

    private bool _sessionActive;

    // This flag is used to identify Debug|NonDebug cases
    private bool _isRunningUnderDebugger;
    private IDeltaApplier? _deltaApplier;

    public ProjectHotReloadSession(
        string name,
        int variant,
        string runtimeVersion,
        Lazy<IHotReloadAgentManagerClient> hotReloadAgentManagerClient,
        Lazy<IHotReloadDiagnosticOutputService> hotReloadOutputService,
        Lazy<IManagedDeltaApplierCreator> deltaApplierCreator,
        IProjectHotReloadSessionCallback callback)
    {
        Name = name;
        _variant = variant.ToString();
        _runtimeVersion = runtimeVersion;
        _hotReloadAgentManagerClient = hotReloadAgentManagerClient;
        _hotReloadOutputService = hotReloadOutputService;
        _deltaApplierCreator = deltaApplierCreator;
        _callback = callback;
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

    // TODO: remove when Web Tools is no longer calling this method.
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
        await _hotReloadAgentManagerClient.Value.AgentStartedAsync(this, flags, cancellationToken);

        WriteToOutputWindow(VSResources.HotReloadStartSession, default);
        _sessionActive = true;
        _isRunningUnderDebugger = runningUnderDebugger;
        EnsureDeltaApplierForSession();
    }

    public async Task StopSessionAsync(CancellationToken cancellationToken)
    {
        if (_sessionActive)
        {
            _sessionActive = false;
            await _hotReloadAgentManagerClient.Value.AgentTerminatedAsync(this, cancellationToken);

            WriteToOutputWindow(VSResources.HotReloadStopSession, default);
        }
    }

    // IManagedHotReloadAgent

    public async ValueTask ApplyUpdatesAsync(ImmutableArray<ManagedHotReloadUpdate> updates, CancellationToken cancellationToken)
    {
        if (!_sessionActive)
        {
            WriteToOutputWindow($"{nameof(ApplyUpdatesAsync)} called but the session is not active.", default, HotReloadVerbosity.Detailed);
            return;
        }

        if (_deltaApplier is null)
        {
            WriteToOutputWindow($"{nameof(ApplyUpdatesAsync)} called but we have no delta applier.", default, HotReloadVerbosity.Detailed);
            return;
        }

        try
        {
            WriteToOutputWindow(VSResources.HotReloadSendingUpdates, cancellationToken, HotReloadVerbosity.Detailed);

            ApplyResult result = await _deltaApplier.ApplyUpdatesAsync(updates, cancellationToken);

            if (result is ApplyResult.Success or ApplyResult.SuccessRefreshUI)
            {
                WriteToOutputWindow(VSResources.HotReloadApplyUpdatesSuccessful, cancellationToken, HotReloadVerbosity.Detailed);

                if (_callback is not null)
                {
                    await _callback.OnAfterChangesAppliedAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            WriteToOutputWindow(
                string.Format(VSResources.HotReloadApplyUpdatesFailure, $"{ex.GetType()}: {ex.Message}"),
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
        WriteToOutputWindow(VSResources.HotReloadErrorsInApplication, cancellationToken, errorLevel: HotReloadDiagnosticErrorLevel.Error);

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
        WriteToOutputWindow(VSResources.HotReloadRestartInProgress, cancellationToken);

        if (_callback is IProjectHotReloadSessionCallback2 callBack2)
        {
            await callBack2.RestartProjectAsync(_isRunningUnderDebugger, cancellationToken);
        }
        else
        {
            await _callback.RestartProjectAsync(cancellationToken);
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        WriteToOutputWindow(VSResources.HotReloadStoppingApplication, cancellationToken);

        await _callback.StopProjectAsync(cancellationToken);

        // TODO: Should we stop the session here? Or does someone else do it?
    }

    public ValueTask<bool> SupportsRestartAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<bool>(_callback.SupportsRestart);
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
