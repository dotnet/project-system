// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.DotNet.HotReload;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

internal sealed class DeltaApplier(HotReloadClient client, IHotReloadDebugStateProvider debugStateProvider) : IDeltaApplierInternal, IStaticAssetApplier
{
    public void Dispose()
    {
        client.Dispose();
    }

    public ValueTask<bool> ApplyProcessEnvironmentVariablesAsync(IDictionary<string, string> envVars, CancellationToken cancellationToken)
    {
        client.ConfigureLaunchEnvironment(envVars);
#if DEBUG
        envVars[AgentEnvironmentVariables.HotReloadDeltaClientLogMessages] = "[Agent] ";
#endif
        return new(true);
    }

    public ValueTask InitiateConnectionAsync(CancellationToken cancellationToken)
    {
        client.InitiateConnection(cancellationToken);
        return new();
    }

    public async ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        => await client.GetUpdateCapabilitiesAsync(cancellationToken);

    public async ValueTask InitializeApplicationAsync(CancellationToken cancellationToken)
    {
        // Not all clients respond correctly to cancellation tokens.
        // For example, `DefaultHotreloadClient.GetUpdateCapabilitiesAsync(ct)`doesn't listen to the passed ct.
        // Since DefaultHotreloadClient is defined in a source package, it can't be modified directly from project-system.
        // Work around this by creating a TaskCompletionSource that completes when the token is cancelled.
        TaskCompletionSource tcs = new TaskCompletionSource();
        cancellationToken.Register(() => tcs.TrySetResult());
        _ = Task.WaitAny(client.GetUpdateCapabilitiesAsync(cancellationToken), tcs.Task);

        // TODO: apply initial updates?
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2571676

        await client.InitialUpdatesAppliedAsync(cancellationToken);
    }

    public async ValueTask<ApplyResult> ApplyUpdatesAsync(ImmutableArray<ManagedHotReloadUpdate> updates, CancellationToken cancellationToken)
    {
        var isProcessSuspended = await debugStateProvider.IsSuspendedAsync(cancellationToken);

        var managedCodeUpdates = ImmutableArray.CreateRange(updates,
            update => new HotReloadManagedCodeUpdate(
                update.Module,
                update.MetadataDelta,
                update.ILDelta,
                update.PdbDelta,
                update.UpdatedTypes,
                update.RequiredCapabilities));

        var status = await client.ApplyManagedCodeUpdatesAsync(managedCodeUpdates, isProcessSuspended, cancellationToken);

        return ToResult(status);
    }

    public async ValueTask<ApplyResult> ApplyStaticFileUpdateAsync(string assemblyName, bool isApplicationProject, string relativePath, byte[] contents, CancellationToken cancellationToken)
    {
        var isProcessSuspended = await debugStateProvider.IsSuspendedAsync(cancellationToken);

        var status = await client.ApplyStaticAssetUpdatesAsync(
            [new HotReloadStaticAssetUpdate(assemblyName, relativePath, [.. contents], isApplicationProject)],
            isProcessSuspended,
            cancellationToken);

        return ToResult(status);
    }

    private static ApplyResult ToResult(ApplyStatus status)
        => status switch
        {
            ApplyStatus.AllChangesApplied or ApplyStatus.SomeChangesApplied or ApplyStatus.NoChangesApplied => ApplyResult.Success,
            _ => ApplyResult.Failed
        };
}
