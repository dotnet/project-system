// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Utilities.DataFlowExtensions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// <see cref="StartupProjectRegistrar"/> is responsible for adding or removing a project from the Startup list
    /// depending on whether the active configuration of the a project is debuggable or not.
    /// </summary>
    internal class StartupProjectRegistrar : OnceInitializedOnceDisposedAsync
    {
        private readonly IVsStartupProjectsListService _startupProjectsListService;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IProjectThreadingService _threadingService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly ActiveConfiguredProject<DebuggerLaunchProviders> _launchProviders;

        private Guid _guid;
        private IDisposable _evaluationSubscriptionLink;
        private bool _isDebuggable;

        internal DataFlowExtensionMethodCaller WrapperMethodCaller { get; set; }

        [ImportingConstructor]
        public StartupProjectRegistrar(
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            IProjectThreadingService threadingService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            ActiveConfiguredProject<DebuggerLaunchProviders> launchProviders)
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));
            Requires.NotNull(launchProviders, nameof(launchProviders));

            _projectVsServices = projectVsServices;
            _startupProjectsListService = serviceProvider.GetService<IVsStartupProjectsListService, SVsStartupProjectsListService>();
            _threadingService = threadingService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _launchProviders = launchProviders;

            WrapperMethodCaller = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapper());
        }

        [ProjectAutoLoad]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        internal async Task OnProjectFactoryCompletedAsync()
        {
            await InitializeCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            ConfigurationGeneral projectProperties =
                await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            _guid = new Guid((string)await projectProperties.ProjectGuid.GetValueAsync().ConfigureAwait(false));
            Assumes.False(_guid == Guid.Empty);

            await InitializeAsync().ConfigureAwait(false);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _evaluationSubscriptionLink?.Dispose();
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            await AddOrRemoveProjectFromStartupProjectList(initialize: true).ConfigureAwait(false);

            var watchedEvaluationRules = Empty.OrdinalIgnoreCaseStringSet.Add(ConfigurationGeneral.SchemaName);
            var evaluationBlock = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                ConfigurationGeneralRuleBlock_ChangedAsync);
            IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> sourceBlock = 
                _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock;
            _evaluationSubscriptionLink = WrapperMethodCaller.LinkTo(
                sourceBlock,
                evaluationBlock,
                ruleNames: watchedEvaluationRules, suppressVersionOnlyUpdates:true
                );
        }

        internal async Task ConfigurationGeneralRuleBlock_ChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            IProjectChangeDescription projectChange = e.Value.ProjectChanges[ConfigurationGeneral.SchemaName];

            /* Currently  we watch for the change in the OutputType to check if a project is debuggable.
               There are other cases where the OutputType will remain the same and still the ability to debuggable a project could change
               For eg: A project's OutputType could be a Lib and an execution entry point could be added or removed
               Tracking bug: https://github.com/dotnet/roslyn-project-system/issues/455
               */
            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.OutputTypeProperty))
            {
                await AddOrRemoveProjectFromStartupProjectList().ConfigureAwait(false);
            }
        }

        private async Task AddOrRemoveProjectFromStartupProjectList(bool initialize = false)
        {
            await _threadingService.SwitchToUIThread();
            bool isDebuggable = await IsDebuggable().ConfigureAwait(true);

            if (initialize || isDebuggable != _isDebuggable)
            {
                _isDebuggable = isDebuggable;
                if (isDebuggable)
                {
                    _startupProjectsListService.AddProject(ref _guid);
                }
                else
                {
                    _startupProjectsListService.RemoveProject(ref _guid);
                }
            }
        }

        private async Task<bool> IsDebuggable()
        {
            foreach (var provider in _launchProviders.Value.Debuggers)
            {
                if (await provider.Value.CanLaunchAsync(DebugLaunchOptions.DesignTimeExpressionEvaluation)
                                        .ConfigureAwait(true))
                {
                    return true;
                }
            }

            return false;
        }

        // Creating a class which provides the LaunchProviders is a workaround because importing ActiveConfiguredProject
        // with 2 arguments does not work.
        [Export]
        internal class DebuggerLaunchProviders
        {
            [ImportingConstructor]
            public DebuggerLaunchProviders(ConfiguredProject project)
            {
                Debuggers = new OrderPrecedenceImportCollection<IDebugLaunchProvider, IDebugLaunchProviderMetadataView>(projectCapabilityCheckProvider: project);
            }

            [ImportMany]
            public OrderPrecedenceImportCollection<IDebugLaunchProvider, IDebugLaunchProviderMetadataView> Debuggers
            {
                get;
            }
        }
    }
}
