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
    using TIdentityDictionary = IImmutableDictionary<NamedIdentity, IComparable>;

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
        private readonly ActiveConfiguredProjectIgnoringTargetFrameworkProvider _targetFrameworkConfiguredProjectProvider;

        private IDisposable _evaluationSubscriptionLink;
        private IDisposable _designTimeBuildSubscriptionLink;

        // TargetFrameworks and the associated AggregateWorkspaceProjectContext for the current project state.
        private string _latestTargetFrameworks;
        private AggregateWorkspaceProjectContext _projectContext;

        [ImportingConstructor]
        public LanguageServiceHost(IUnconfiguredProjectCommonServices commonServices,
                                   Lazy<IProjectContextProvider> contextProvider,
                                   [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
                                   IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
                                   ActiveConfiguredProjectIgnoringTargetFrameworkProvider targetFrameworkConfiguredProjectProvider)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(contextProvider, nameof(contextProvider));
            Requires.NotNull(tasksService, nameof(tasksService));
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));
            Requires.NotNull(targetFrameworkConfiguredProjectProvider, nameof(targetFrameworkConfiguredProjectProvider));

            _commonServices = commonServices;
            _tasksService = tasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _projectContextProvider = new AggregateWorkspaceProjectContextProvider(contextProvider);
            _targetFrameworkConfiguredProjectProvider = targetFrameworkConfiguredProjectProvider;

            Handlers = new OrderPrecedenceImportCollection<ILanguageServiceRuleHandler>(projectCapabilityCheckProvider: commonServices.Project);
        }

        public object HostSpecificErrorReporter
        {
            get { return _projectContext?.HostSpecificErrorReporter; }
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

                _designTimeBuildSubscriptionLink = _activeConfiguredProjectSubscriptionService.JointRuleSource.SourceBlock.LinkTo(
                  new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChangedAsync(e, RuleHandlerType.DesignTimeBuild)),
                  ruleNames: watchedDesignTimeBuildRules.Union(watchedEvaluationRules), suppressVersionOnlyUpdates: true);

                _evaluationSubscriptionLink = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChangedAsync(e, RuleHandlerType.Evaluation)),
                    ruleNames: watchedEvaluationRules, suppressVersionOnlyUpdates: true);
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

            // If "TargetFrameworks" property has changed, we need to refresh the project context and subscriptions.
            string newTargetFrameworks;
            if (HasTargetFrameworksChanged(e, out newTargetFrameworks))
            {
                await UpdateProjectContextAndSubscriptionsAsync(newTargetFrameworks).ConfigureAwait(false);
            }

            // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
            await _commonServices.ThreadingService.SwitchToUIThread();

            using (_tasksService.LoadedProject())
            {
                await HandleAsync(e, handlerType, _projectContext).ConfigureAwait(false);
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
                if (newProjectContext == _projectContext)
                {
                    // Another thread has already completed the update.
                    Requires.Range(string.Equals(targetFrameworks, _latestTargetFrameworks, StringComparison.OrdinalIgnoreCase), nameof(targetFrameworks));
                    return;
                }

                previousProjectContext = _projectContext;
                _projectContext = newProjectContext;
                _latestTargetFrameworks = targetFrameworks;
            }

            await ResetSubscriptionsAsync().ConfigureAwait(false);

            foreach (var innerContext in previousProjectContext?.InnerProjectContexts)
            {
                foreach (var handler in Handlers)
                {
                    await handler.Value.OnContextReleasedAsync(innerContext).ConfigureAwait(false);
                }
            }
        }

        private async Task ResetSubscriptionsAsync()
        {
            // TODO: Design time builds for all active configured projects (https://github.com/dotnet/roslyn-project-system/issues/532)
            //_designTimeBuildSubscriptionLink?.Dispose();
            _evaluationSubscriptionLink?.Dispose();

            using (_tasksService.LoadedProject())
            {
                var currentProjects = await _targetFrameworkConfiguredProjectProvider.GetConfiguredProjectsAsync().ConfigureAwait(false);

                // TODO: Design time builds for all active configured projects (https://github.com/dotnet/roslyn-project-system/issues/532)
                //var sourceBlocks = currentProjects.Select(
                //    cp => cp.Services.ProjectSubscription.JointRuleSource.SourceBlock.SyncLinkOptions<IProjectValueVersions>());
                //var target = new ActionBlock<Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary>>(s => ProjectPropertyChangedAsync(s, RuleHandlerType.DesignTimeBuild));
                //_designTimeBuildSubscriptionLink = ProjectDataSources.SyncLinkTo(sourceBlocks.ToImmutableList(), target, null);

                var sourceBlocks = currentProjects.Select(
                    cp => cp.Services.ProjectSubscription.ProjectRuleSource.SourceBlock.SyncLinkOptions<IProjectValueVersions>());
                var target = new ActionBlock<Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary>>(s => ProjectPropertyChangedAsync(s, RuleHandlerType.Evaluation));
                _evaluationSubscriptionLink = ProjectDataSources.SyncLinkTo(sourceBlocks.ToImmutableList(), target, null);
            }
        }

        private async Task ProjectPropertyChangedAsync(Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary> sources, RuleHandlerType handlerType)
        {
            foreach (IProjectVersionedValue<IProjectSubscriptionUpdate> change in sources.Item1)
            {
                await OnProjectChangedAsync(change, handlerType).ConfigureAwait(false);
            }
        }

        private async Task HandleAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update, RuleHandlerType handlerType, AggregateWorkspaceProjectContext projectContext)
        {
            var handlers = Handlers.Select(h => h.Value)
                                   .Where(h => h.HandlerType == handlerType);

            // Get the inner workspace project context(s) to update.
            var workspaceProjectContextsToUpdate = GetWorkspaceProjectContextsToUpdate(update);

            foreach (var handler in handlers)
            {
                IProjectChangeDescription projectChange = update.Value.ProjectChanges[handler.RuleName];
                if (projectChange.Difference.AnyChanges)
                {
                    foreach (var context in workspaceProjectContextsToUpdate)
                    {
                        await handler.HandleAsync(update, projectChange, context)
                                     .ConfigureAwait(false);
                    }
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

        private ImmutableArray<IWorkspaceProjectContext> GetWorkspaceProjectContextsToUpdate(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
        {
            // Get the set of workspace project contexts to update for the changed ProjectConfiguration.
            var contextBuilder = ImmutableArray.CreateBuilder<IWorkspaceProjectContext>();
            if (update.Value.ProjectConfiguration.IsCrossTargeting())
            {
                // This change affects a specific TargetFramework for a cross-targeting project.
                var contextToUpdate = _projectContext.GetProjectContext(update.Value.ProjectConfiguration);
                contextBuilder.Add(contextToUpdate);
            }
            else
            {
                // We either have a project targeting a single framework OR a change that affects all target frameworks of a cross-targeting project.
                // For both these cases, we need to update all the inner workspace project contexts.
                contextBuilder.AddRange(_projectContext.InnerProjectContexts);
            }

            return contextBuilder.ToImmutable();
        }

        private bool HasTargetFrameworksChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> e, out string targetFrameworks)
        {
            IProjectChangeDescription projectChange = e.Value.ProjectChanges[ConfigurationGeneral.SchemaName];
            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworksProperty))
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
                _evaluationSubscriptionLink?.Dispose();
                _designTimeBuildSubscriptionLink?.Dispose();

                await _projectContextProvider.DisposeAsync().ConfigureAwait(false); ;
            }
        }
    }
}
