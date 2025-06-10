// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

/// <summary>
/// Adds or removes a project from <see cref="IVsStartupProjectsListService" /> based on whether the project is debuggable or not.
/// </summary>
/// <summary>
/// <para>
/// This is an unconfigured project scoped <see cref="ProjectAutoLoadAttribute"/> component for
/// <see cref="ProjectCapability.DotNet"/> projects, and initializes after the project factory completes.
/// </para>
/// <para>
/// It subscribes to project data for the active configuration and reevalutes on every update. Data provided by the
/// subscription is not used directly. Instead, the downstream logic obtains its own data once triggered by this class.
/// </para>
/// </summary>
[method: ImportingConstructor]
internal sealed class StartupProjectRegistrar(
    UnconfiguredProject project,
    IUnconfiguredProjectTasksService projectTasksService,
    IVsService<SVsStartupProjectsListService, IVsStartupProjectsListService> startupProjectsListService,
    IProjectThreadingService threadingService,
    ISafeProjectGuidService projectGuidService,
    IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
    IActiveConfiguredValues<IDebugLaunchProvider> launchProviders)
    : OnceInitializedOnceDisposedAsync(threadingService.JoinableTaskContext)
{
    private Guid _projectGuid;
    private IDisposable? _subscription;

    [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
    [AppliesTo(ProjectCapability.DotNet)]
    public Task InitializeAsync()
    {
        threadingService.RunAndForget(async () =>
        {
            await projectTasksService.SolutionLoadedInHost;

            await InitializeAsync(CancellationToken.None);
        }, project);

        return Task.CompletedTask;
    }

    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _projectGuid = await projectGuidService.GetProjectGuidAsync(cancellationToken);

        Assumes.False(_projectGuid == Guid.Empty, "Project GUID cannot be empty.");

        _subscription = projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkToAsyncAction(
            target: OnProjectChangedAsync,
            project);
    }

    protected override Task DisposeCoreAsync(bool initialized)
    {
        if (initialized)
        {
            _subscription?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate>? _ = null)
    {
        // Ensure the project doesn't unload while we're computing this.
        return projectTasksService.LoadedProjectAsync(async () =>
        {
            IVsStartupProjectsListService? startupList = await startupProjectsListService.GetValueOrNullAsync();

            if (startupList is null)
            {
                return;
            }

            bool isDebuggable = await IsDebuggableAsync();

            if (isDebuggable)
            {
                // If we're already registered, the service no-ops
                startupList.AddProject(ref _projectGuid);
            }
            else
            {
                // If we're already unregistered, the service no-ops
                startupList.RemoveProject(ref _projectGuid);
            }
        });
    }

    private async Task<bool> IsDebuggableAsync()
    {
        bool foundStartupProjectProvider = false;

        foreach (Lazy<IDebugLaunchProvider> provider in launchProviders.Values)
        {
            if (provider.Value is IStartupProjectProvider startupProjectProvider)
            {
                foundStartupProjectProvider = true;

                if (await startupProjectProvider.CanBeStartupProjectAsync(DebugLaunchOptions.DesignTimeExpressionEvaluation))
                {
                    return true;
                }
            }
        }

        if (!foundStartupProjectProvider)
        {
            foreach (Lazy<IDebugLaunchProvider> provider in launchProviders.Values)
            {
                if (await provider.Value.CanLaunchAsync(DebugLaunchOptions.DesignTimeExpressionEvaluation))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
