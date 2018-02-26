// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

using AsyncServiceProvider = Microsoft.VisualStudio.Shell.AsyncServiceProvider;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    ///     Responsible for adding or removing the project from the startup list based on whether the project
    ///     is debuggable or not.
    /// </summary>
    internal class StartupProjectRegistrar : OnceInitializedOnceDisposedAsync
    {
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IProjectThreadingService _threadingService;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly ActiveConfiguredProject<DebuggerLaunchProviders> _launchProviders;
        private readonly IProjectGuidService2 _projectGuidService;
        private IVsStartupProjectsListService _startupProjectsListService;
        private Guid _projectGuid;
        private IDisposable _subscription;

        [ImportingConstructor]
        public StartupProjectRegistrar(
            IProjectThreadingService threadingService,
            [Import(typeof(IProjectGuidService))]IProjectGuidService2 projectGuidService,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            ActiveConfiguredProject<DebuggerLaunchProviders> launchProviders)
            : this(AsyncServiceProvider.GlobalProvider, threadingService, projectGuidService, projectSubscriptionService, launchProviders)
        {
        }

        public StartupProjectRegistrar(
            IAsyncServiceProvider serviceProvider,
            IProjectThreadingService threadingService,
            IProjectGuidService2 projectGuidService,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            ActiveConfiguredProject<DebuggerLaunchProviders> launchProviders)
        : base(threadingService.JoinableTaskContext)
        {
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
            _projectGuidService = projectGuidService;
            _projectSubscriptionService = projectSubscriptionService;
            _launchProviders = launchProviders;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
        public Task InitializeAsync()
        {
            return InitializeAsync(CancellationToken.None);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _projectGuid = await _projectGuidService.GetProjectGuidAsync()
                                                    .ConfigureAwait(false);

            Assumes.False(_projectGuid == Guid.Empty);

            _startupProjectsListService = (IVsStartupProjectsListService)await _serviceProvider.GetServiceAsync(typeof(SVsStartupProjectsListService))
                                                                                               .ConfigureAwait(false);

            Assumes.Present(_startupProjectsListService);

            _subscription = _projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                target: new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(OnProjectChangedAsync),
                suppressVersionOnlyUpdates: true,
                linkOptions: new DataflowLinkOptions() { PropagateCompletion = true });
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                _subscription.Dispose();
            }

            return Task.CompletedTask;
        }

        internal async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            bool isDebuggable = await _launchProviders.Value.IsDebuggableAsync()
                                                            .ConfigureAwait(false);

            if (isDebuggable)
            {
                // If we're already registered, the service no-ops
                _startupProjectsListService.AddProject(ref _projectGuid);
            }
            else
            {
                // If we're already unregistered, the service no-ops
                _startupProjectsListService.RemoveProject(ref _projectGuid);
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
