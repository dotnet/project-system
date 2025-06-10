// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

/// <summary>
///     Responsible for adding or removing the project from the startup list based on whether the project
///     is debuggable or not.
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

        Assumes.False(_projectGuid == Guid.Empty);

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
