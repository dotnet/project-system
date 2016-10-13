// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Hosts a <see cref="IWorkspaceProjectContext"/> and handles the interaction between the project system and the language service.
    /// </summary>
    [Export(typeof(ILanguageServiceHost))]
    internal partial class LanguageServiceHost : OnceInitializedOnceDisposedAsync, ILanguageServiceHost
    {
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly AggregateWorkspaceProjectContextProvider _projectContextProvider;
        private readonly ActiveConfiguredProjectsProvider _activeConfiguredProjectsProvider;

        private readonly List<IDisposable> _evaluationSubscriptionLinks;
        private readonly List<IDisposable> _designTimeBuildSubscriptionLinks;

        // TargetFrameworks and the associated AggregateWorkspaceProjectContext for the current project state.
        private string _latestTargetFrameworks;
        private AggregateWorkspaceProjectContext _aggregateProjectContext;

        [ImportingConstructor]
        public LanguageServiceHost(IUnconfiguredProjectCommonServices commonServices,
                                   Lazy<IProjectContextProvider> contextProvider,
                                   [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
                                   IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
                                   ActiveConfiguredProjectsProvider activeConfiguredProjectsProvider)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(contextProvider, nameof(contextProvider));
            Requires.NotNull(tasksService, nameof(tasksService));
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));
            Requires.NotNull(activeConfiguredProjectsProvider, nameof(activeConfiguredProjectsProvider));

            _commonServices = commonServices;
            _tasksService = tasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _projectContextProvider = new AggregateWorkspaceProjectContextProvider(contextProvider);
            _activeConfiguredProjectsProvider = activeConfiguredProjectsProvider;

            Handlers = new OrderPrecedenceImportCollection<ILanguageServiceRuleHandler>(projectCapabilityCheckProvider: commonServices.Project);
            _evaluationSubscriptionLinks = new List<IDisposable>();
            _designTimeBuildSubscriptionLinks = new List<IDisposable>();
        }

        public object HostSpecificErrorReporter
        {
            get { return _aggregateProjectContext?.HostSpecificErrorReporter; }
        }

        public IWorkspaceProjectContext ActiveProjectContext
        {
            get { return _aggregateProjectContext.ActiveProjectContext; }
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<ILanguageServiceRuleHandler> Handlers
        {
            get;
        }

        [ProjectAutoLoad(ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
        private Task OnProjectFactoryCompletedAsync()
        {
            using (_tasksService.LoadedProject())
            {
                var watchedEvaluationRules = GetWatchedRules(RuleHandlerType.Evaluation);
                var watchedDesignTimeBuildRules = GetWatchedRules(RuleHandlerType.DesignTimeBuild);

                _designTimeBuildSubscriptionLinks.Add(_activeConfiguredProjectSubscriptionService.JointRuleSource.SourceBlock.LinkTo(
                  new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChangedAsync(e, RuleHandlerType.DesignTimeBuild)),
                  ruleNames: watchedDesignTimeBuildRules.Union(watchedEvaluationRules), suppressVersionOnlyUpdates: true));

                _evaluationSubscriptionLinks.Add(_activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChangedAsync(e, RuleHandlerType.Evaluation)),
                    ruleNames: watchedEvaluationRules, suppressVersionOnlyUpdates: true));
            }

            return Task.CompletedTask;
        }

        protected async override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // Don't initialize if we're unloading
            _tasksService.UnloadCancellationToken.ThrowIfCancellationRequested();

            // Set the project context for the latest TargetFrameworks for this project.
            var projectProperties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            var targetFrameworks = (string)await projectProperties.TargetFrameworks.GetValueAsync().ConfigureAwait(false);
            await UpdateProjectContextAndSubscriptionsAsync(targetFrameworks).ConfigureAwait(false);
        }

        private async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e, RuleHandlerType handlerType)
        {
            if (IsDisposing || IsDisposed)
                return;

            await InitializeAsync().ConfigureAwait(false);

            await OnProjectChangedCoreAsync(e, handlerType).ConfigureAwait(false);
        }

        private async Task OnProjectChangedCoreAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e, RuleHandlerType handlerType)
        {
            // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
            await _commonServices.ThreadingService.SwitchToUIThread();

            using (_tasksService.LoadedProject())
            {
                await HandleAsync(e, handlerType, _aggregateProjectContext).ConfigureAwait(false);
            }

            // If "TargetFrameworks" property has changed, we need to refresh the project context and subscriptions.
            string newTargetFrameworks;
            if (HasTargetFrameworksChanged(e, out newTargetFrameworks))
            {
                await UpdateProjectContextAndSubscriptionsAsync(newTargetFrameworks).ConfigureAwait(false);
            }
        }

        private async Task UpdateProjectContextAndSubscriptionsAsync(string targetFrameworks)
        {
            lock (_projectContextProvider)
            {
                if (string.Equals(targetFrameworks, _latestTargetFrameworks, StringComparison.OrdinalIgnoreCase))
                {
                    // We have already handled this targetFrameworks update.
                    return;
                }
            }

            var newProjectContext = await _projectContextProvider.UpdateProjectContextAsync(targetFrameworks, CancellationToken.None).ConfigureAwait(false);

            AggregateWorkspaceProjectContext previousProjectContext;
            lock (_projectContextProvider)
            {
                if (newProjectContext == _aggregateProjectContext)
                {
                    // Another thread has already completed the update.
                    Requires.Range(string.Equals(targetFrameworks, _latestTargetFrameworks, StringComparison.OrdinalIgnoreCase), nameof(targetFrameworks));
                    return;
                }

                previousProjectContext = _aggregateProjectContext;
                _aggregateProjectContext = newProjectContext;
                _latestTargetFrameworks = targetFrameworks;
            }

            await ResetSubscriptionsAsync().ConfigureAwait(false);

            if (previousProjectContext != null)
            {
                foreach (var innerContext in previousProjectContext.InnerProjectContexts)
                {
                    foreach (var handler in Handlers)
                    {
                        await handler.Value.OnContextReleasedAsync(innerContext).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task ResetSubscriptionsAsync()
        {
            DisposeAndClearSubscriptions();

            var activeConfiguredProjects = await _activeConfiguredProjectsProvider.GetActiveConfiguredProjectsAsync().ConfigureAwait(false);
            using (_tasksService.LoadedProject())
            {
                var watchedEvaluationRules = GetWatchedRules(RuleHandlerType.Evaluation);
                var watchedDesignTimeBuildRules = GetWatchedRules(RuleHandlerType.DesignTimeBuild);
                
                foreach (var project in activeConfiguredProjects)
                {
                    _designTimeBuildSubscriptionLinks.Add(project.Services.ProjectSubscription.JointRuleSource.SourceBlock.LinkTo(
                        new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChangedCoreAsync(e, RuleHandlerType.DesignTimeBuild)),
                        ruleNames: watchedDesignTimeBuildRules.Union(watchedEvaluationRules), suppressVersionOnlyUpdates: true));

                    _evaluationSubscriptionLinks.Add(project.Services.ProjectSubscription.ProjectRuleSource.SourceBlock.LinkTo(
                        new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChangedCoreAsync(e, RuleHandlerType.Evaluation)),
                        ruleNames: watchedEvaluationRules, suppressVersionOnlyUpdates: true));
                }
            }
        }

        private async Task HandleAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update, RuleHandlerType handlerType, AggregateWorkspaceProjectContext projectContext)
        {
            var handlers = Handlers.Select(h => h.Value)
                                   .Where(h => h.HandlerType == handlerType);

            // Get the inner workspace project context to update for this change.
            var projectContextToUpdate = _aggregateProjectContext?.GetProjectContext(update.Value.ProjectConfiguration);
            if (projectContextToUpdate == null)
            {
                return;
            }

            // Broken design time builds sometimes cause updates with no project changes and sometimes cause updates with a project change that has no difference.
            // We handle the former case here, and the latter case is handled in the CommandLineItemHandler.
            if (update.Value.ProjectChanges.Count == 0)
            {
                if (handlerType == RuleHandlerType.DesignTimeBuild)
                {
                    projectContextToUpdate.LastDesignTimeBuildSucceeded = false;
                }

                return;
            }

            foreach (var handler in handlers)
            {
                IProjectChangeDescription projectChange = update.Value.ProjectChanges[handler.RuleName];
                if (handler.ReceiveUpdatesWithEmptyProjectChange || projectChange.Difference.AnyChanges)
                {
                    await handler.HandleAsync(update, projectChange, projectContextToUpdate).ConfigureAwait(false);
                }
            }
        }

        private IEnumerable<string> GetWatchedRules(RuleHandlerType handlerType)
        {
            return Handlers.Where(h => h.Value.HandlerType == handlerType)
                           .Select(h => h.Value.RuleName)
                           .Distinct(StringComparers.RuleNames)
                           .ToArray();
        }

        private bool HasTargetFrameworksChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> e, out string targetFrameworks)
        {
            IProjectChangeDescription projectChange;
            if (e.Value.ProjectChanges.TryGetValue(ConfigurationGeneral.SchemaName, out projectChange) &&
                projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworksProperty))
            {
                targetFrameworks = projectChange.After.Properties[ConfigurationGeneral.TargetFrameworksProperty];
                return true;
            }

            targetFrameworks = null;
            return false;
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                DisposeAndClearSubscriptions();
                await _projectContextProvider.DisposeAsync().ConfigureAwait(false);
            }
        }

        private void DisposeAndClearSubscriptions()
        {
            foreach (var link in _evaluationSubscriptionLinks.Concat(_designTimeBuildSubscriptionLinks))
            {
                link.Dispose();
            }

            _evaluationSubscriptionLinks.Clear();
            _designTimeBuildSubscriptionLinks.Clear();
        }
    }
}
