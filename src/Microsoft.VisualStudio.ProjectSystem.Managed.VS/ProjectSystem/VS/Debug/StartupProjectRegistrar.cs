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
        private IVsStartupProjectsListService _startupProjectsListService;
        private Guid _projectGuid;
#pragma warning disable CA2213 // OnceInitializedOnceDisposedAsync are not tracked corretly by the IDisposeable analyzer
        private IDisposable _subscription;
#pragma warning restore CA2213

        [ImportingConstructor]
        public StartupProjectRegistrar(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            ActiveConfiguredProject<DebuggerLaunchProviders> launchProviders)
            : this(project, AsyncServiceProvider.GlobalProvider, threadingService, projectSubscriptionService, launchProviders)
        {
        }

        public StartupProjectRegistrar(
            UnconfiguredProject project,
            IAsyncServiceProvider serviceProvider,
            IProjectThreadingService threadingService,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            ActiveConfiguredProject<DebuggerLaunchProviders> launchProviders)
        : base(threadingService.JoinableTaskContext)
        {
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
            _projectSubscriptionService = projectSubscriptionService;
            _launchProviders = launchProviders;

            ProjectGuidServices = new OrderPrecedenceImportCollection<IProjectGuidService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectGuidService> ProjectGuidServices
        {
            get;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
        public Task InitializeAsync()
        {
            return InitializeAsync(CancellationToken.None);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            IProjectGuidService2 projectGuidService = ProjectGuidServices.FirstOrDefault()?.Value as IProjectGuidService2;
            if (projectGuidService == null)
                return;

            _projectGuid = await projectGuidService.GetProjectGuidAsync()
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
                _subscription?.Dispose();
            }

            return Task.CompletedTask;
        }

        internal async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e = null)
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
