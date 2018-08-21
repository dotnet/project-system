// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly IVsService<IVsStartupProjectsListService> _startupProjectsListService;
        private readonly ISafeProjectGuidService _projectGuidService;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly ActiveConfiguredProject<DebuggerLaunchProviders> _launchProviders;
        
        private Guid _projectGuid;
#pragma warning disable CA2213 // OnceInitializedOnceDisposedAsync are not tracked corretly by the IDisposeable analyzer
        private IDisposable _subscription;
#pragma warning restore CA2213

        [ImportingConstructor]
        public StartupProjectRegistrar(
            UnconfiguredProject project,
            IVsService<SVsStartupProjectsListService, IVsStartupProjectsListService> startupProjectsListService,
            IProjectThreadingService threadingService,
            ISafeProjectGuidService projectGuidService,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            ActiveConfiguredProject<DebuggerLaunchProviders> launchProviders)
        : base(threadingService.JoinableTaskContext)
        {
            _startupProjectsListService = startupProjectsListService;
            _projectGuidService = projectGuidService;
            _projectSubscriptionService = projectSubscriptionService;
            _launchProviders = launchProviders;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.DotNet)]
        public Task InitializeAsync()
        {
            return InitializeAsync(CancellationToken.None);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _projectGuid = await _projectGuidService.GetProjectGuidAsync()
                                                    .ConfigureAwait(false);

            Assumes.False(_projectGuid == Guid.Empty);

            _subscription = _projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkToAsyncAction(
                target: OnProjectChangedAsync);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                _subscription?.Dispose();
            }

            return Task.CompletedTask;
        }

        internal async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e = null)
        {
            bool isDebuggable = await _launchProviders.Value.IsDebuggableAsync()
                                                            .ConfigureAwait(false);

            IVsStartupProjectsListService startupProjectsListService = await _startupProjectsListService.GetValueAsync()
                                                                                                        .ConfigureAwait(true);

            Assumes.Present(startupProjectsListService);

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
        }

        [Export]
        internal class DebuggerLaunchProviders
        {
            [ImportingConstructor]
            public DebuggerLaunchProviders(ConfiguredProject project)
            {
                Debuggers = new OrderPrecedenceImportCollection<IDebugLaunchProvider>(projectCapabilityCheckProvider: project);
            }

            [ImportMany]
            public OrderPrecedenceImportCollection<IDebugLaunchProvider> Debuggers
            {
                get;
            }

            public async Task<bool> IsDebuggableAsync()
            {
                foreach (Lazy<IDebugLaunchProvider> provider in Debuggers)
                {
                    if (await provider.Value.CanLaunchAsync(DebugLaunchOptions.DesignTimeExpressionEvaluation)
                                            .ConfigureAwait(false))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
