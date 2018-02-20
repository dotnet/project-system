// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// <see cref="StartupProjectRegistrar"/> is responsible for adding or removing a project from the Startup list
    /// depending on whether the active configuration of the a project is debuggable or not.
    /// </summary>
    internal class StartupProjectRegistrar : OnceInitializedOnceDisposed
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly ActiveConfiguredProject<DebuggerLaunchProviders> _launchProviders;
        private readonly IVsService<IVsStartupProjectsListService> _startupProjectsListService;
        private Guid _guid = Guid.Empty;
        private IDisposable _evaluationSubscriptionLink;
        private bool _isDebuggable;

        [ImportingConstructor]
        public StartupProjectRegistrar(
            IVsService<SVsStartupProjectsListService, IVsStartupProjectsListService> startupProjectsListService,
            IProjectThreadingService threadingService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            ActiveConfiguredProject<DebuggerLaunchProviders> launchProviders)
        {
            _startupProjectsListService = startupProjectsListService;
            _threadingService = threadingService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _launchProviders = launchProviders;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
        internal Task Load()
        {
            EnsureInitialized();
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _evaluationSubscriptionLink?.Dispose();
            }
        }

        protected override void Initialize()
        {
            var watchedEvaluationRules = Empty.OrdinalIgnoreCaseStringSet.Add(ConfigurationGeneral.SchemaName);
            var evaluationBlock = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(ConfigurationGeneralRuleBlock_ChangedAsync);
            _evaluationSubscriptionLink = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock
                                            .LinkTo(target: evaluationBlock, ruleNames: watchedEvaluationRules, suppressVersionOnlyUpdates: true);
        }

        internal async Task ConfigurationGeneralRuleBlock_ChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            IProjectChangeDescription projectChange = e.Value.ProjectChanges[ConfigurationGeneral.SchemaName];

            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.ProjectGuidProperty))
            {
                if (Guid.TryParse(projectChange.After.Properties[ConfigurationGeneral.ProjectGuidProperty], out Guid result))
                {
                    _guid = result;
                    await AddOrRemoveProjectFromStartupProjectListAsync(initialize: true).ConfigureAwait(false);
                }

                return;
            }

            if (_guid == Guid.Empty)
            {
                return;
            }

            /* Currently  we watch for the change in the OutputType to check if a project is debuggable.
               There are other cases where the OutputType will remain the same and still the ability to debuggable a project could change
               For eg: A project's OutputType could be a Lib and an execution entry point could be added or removed
               Tracking bug: https://github.com/dotnet/roslyn-project-system/issues/455
               */
            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.OutputTypeProperty))
            {
                await AddOrRemoveProjectFromStartupProjectListAsync().ConfigureAwait(false);
            }
        }

        private async Task AddOrRemoveProjectFromStartupProjectListAsync(bool initialize = false)
        {
            bool isDebuggable = await IsDebuggableAsync().ConfigureAwait(false);
            await _threadingService.SwitchToUIThread();

            if (initialize || isDebuggable != _isDebuggable)
            {
                _isDebuggable = isDebuggable;
                if (isDebuggable)
                {
                    _startupProjectsListService.Value.AddProject(ref _guid);
                }
                else
                {
                    _startupProjectsListService.Value.RemoveProject(ref _guid);
                }
            }
        }

        private async Task<bool> IsDebuggableAsync()
        {
            foreach (var provider in _launchProviders.Value.Debuggers)
            {
                if (await provider.Value.CanLaunchAsync(DebugLaunchOptions.DesignTimeExpressionEvaluation)
                                        .ConfigureAwait(false))
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
