using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Tasks = System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    internal class StartupProjectRegistrar :
        IDisposable
    {
        private readonly IVsStartupProjectsListService _startupProjectsListService;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IProjectThreadingService _threadingService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly ActiveConfiguredProject<DebuggerLaunchProviders> _launchProviders;

        private Guid _guid;
        private IDisposable _evaluationSubscriptionLink;
        private bool _isDebuggable;

        internal WrapperMethod WrapperMethodCaller { get; set; }

        [ImportingConstructor]
        public StartupProjectRegistrar(
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            IProjectThreadingService threadingService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            ActiveConfiguredProject<DebuggerLaunchProviders> launchProviders)
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

            WrapperMethodCaller = new WrapperMethod(new StaticWrapper());
        }

        [ProjectAutoLoad]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        internal async Tasks.Task OnProjectFactoryCompletedAsync()
        {
            ConfigurationGeneral projectProperties =
                await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            _guid = new Guid((string)await projectProperties.ProjectGuid.GetValueAsync().ConfigureAwait(false));
            Assumes.False(_guid == Guid.Empty);

            await InitializeAsync().ConfigureAwait(false);
        }

        public async Tasks.Task InitializeAsync()
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

        internal async Tasks.Task ConfigurationGeneralRuleBlock_ChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            IProjectChangeDescription projectChange = e.Value.ProjectChanges[ConfigurationGeneral.SchemaName];

            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.OutputTypeProperty))
            {
                await AddOrRemoveProjectFromStartupProjectList().ConfigureAwait(false);
            }
        }

        private async Tasks.Task AddOrRemoveProjectFromStartupProjectList(bool initialize = false)
        {
            bool isDebuggable = await IsDebuggable().ConfigureAwait(false);

            if (initialize || isDebuggable != _isDebuggable)
            {
                _isDebuggable = isDebuggable;
                await _threadingService.SwitchToUIThread();
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

        private async Tasks.Task<bool> IsDebuggable()
        {
            foreach (var provider in _launchProviders.Value.Debuggers)
            {
                if (await provider.Value.CanLaunchAsync(DebugLaunchOptions.StopAtEntryPoint).ConfigureAwait(true))
                {
                    return true;
                }
            }

            return false;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _evaluationSubscriptionLink?.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

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

        internal class WrapperMethod
        {
            private IStaticWrapper _wrapper;

            public WrapperMethod(IStaticWrapper wrapper)
            {
                _wrapper = wrapper;
            }

            public IDisposable LinkTo(
                IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> sourceBlock,
                ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target,
                IEnumerable<string> ruleNames,
                bool suppressVersionOnlyUpdates)
            {
                return _wrapper.LinkTo(sourceBlock, target, ruleNames, suppressVersionOnlyUpdates);
            }
        }

        internal class StaticWrapper : IStaticWrapper
        {
            public IDisposable LinkTo(
                IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> sourceBlock,
                ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target,
                IEnumerable<string> ruleNames,
                bool suppressVersionOnlyUpdates)
            {
                return sourceBlock.LinkTo(
                    target: target,
                    ruleNames: ruleNames,
                    suppressVersionOnlyUpdates: suppressVersionOnlyUpdates);
            }
        }

        public interface IStaticWrapper
        {
            IDisposable LinkTo(
                IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> sourceBlock,
                ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target,
                IEnumerable<string> ruleNames,
                bool suppressVersionOnlyUpdates);
        }
    }
}
