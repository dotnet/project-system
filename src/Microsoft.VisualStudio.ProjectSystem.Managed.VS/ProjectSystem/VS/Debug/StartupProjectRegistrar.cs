// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    ///     Responsible for adding or removing the project from the startup list based on whether the project
    ///     is debuggable or not.
    /// </summary>
    internal class StartupProjectRegistrar : OnceInitializedOnceDisposedAsync
    {
        private readonly UnconfiguredProject _project;
        private readonly IUnconfiguredProjectTasksService _projectTasksService;
        private readonly IVsService<IVsStartupProjectsListService> _startupProjectsListService;
        private readonly IProjectThreadingService _threadingService;
        private readonly ISafeProjectGuidService _projectGuidService;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IActiveConfiguredValues<IDebugLaunchProvider> _launchProviders;

        private Guid _projectGuid;
        private IDisposable? _subscription;

        [ImportingConstructor]
        public StartupProjectRegistrar(
            UnconfiguredProject project,
            IUnconfiguredProjectTasksService projectTasksService,
            IVsService<SVsStartupProjectsListService, IVsStartupProjectsListService> startupProjectsListService,
            IProjectThreadingService threadingService,
            ISafeProjectGuidService projectGuidService,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            IActiveConfiguredValues<IDebugLaunchProvider> launchProviders)
        : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _projectTasksService = projectTasksService;
            _startupProjectsListService = startupProjectsListService;
            _threadingService = threadingService;
            _projectGuidService = projectGuidService;
            _projectSubscriptionService = projectSubscriptionService;
            _launchProviders = launchProviders;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.DotNet)]
        public Task InitializeAsync()
        {
            _threadingService.RunAndForget(async () =>
            {
                await _projectTasksService.SolutionLoadedInHost;

                await InitializeAsync(CancellationToken.None);
            }, _project);

            return Task.CompletedTask;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _projectGuid = await _projectGuidService.GetProjectGuidAsync(cancellationToken);

            Assumes.False(_projectGuid == Guid.Empty);

            _subscription = _projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkToAsyncAction(
                target: OnProjectChangedAsync,
                _project);
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
            return _projectTasksService.LoadedProjectAsync(async () =>
            {
                bool isDebuggable = await IsDebuggableAsync();

                IVsStartupProjectsListService? startupProjectsListService = await _startupProjectsListService.GetValueOrNullAsync();

                if (startupProjectsListService is null)
                {
                    return;
                }

                if (isDebuggable)
                {
                    // If we're already registered, the service no-ops
                    startupProjectsListService.AddProject(ref _projectGuid);
                }
                else
                {
                    // If we're already unregistered, the service no-ops
                    startupProjectsListService.RemoveProject(ref _projectGuid);
                }
            });
        }

        private async Task<bool> IsDebuggableAsync()
        {
            var foundStartupProjectProvider = false;

            foreach (Lazy<IDebugLaunchProvider> provider in _launchProviders.Values)
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
                foreach (Lazy<IDebugLaunchProvider> provider in _launchProviders.Values)
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
}
