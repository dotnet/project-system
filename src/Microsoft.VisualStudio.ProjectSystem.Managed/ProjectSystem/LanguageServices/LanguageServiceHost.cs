// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Hosts a <see cref="IWorkspaceProjectContext"/> and handles the interaction between the project system and the language service.
    /// </summary>
    [Export(typeof(IActiveWorkspaceProjectContextHost))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal partial class LanguageServiceHost : OnceInitializedOnceDisposedAsync, IActiveWorkspaceProjectContextHost
    {
#pragma warning disable CA2213 // OnceInitializedOnceDisposedAsync are not tracked correctly by the IDisposeable analyzer
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
#pragma warning restore CA2213
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly Lazy<IProjectContextProvider> _contextProvider;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IActiveProjectConfigurationRefreshService _activeProjectConfigurationRefreshService;
        private readonly LanguageServiceHandlerManager _languageServiceHandlerManager;
        private readonly IUnconfiguredProjectTasksService _unconfiguredProjectTasksService;
        private readonly List<IDisposable> _evaluationSubscriptionLinks;
        private readonly List<IDisposable> _designTimeBuildSubscriptionLinks;
        private readonly HashSet<ProjectConfiguration> _projectConfigurationsWithSubscriptions;

        private static readonly Lazy<bool> s_workspaceSupportsBatchingAndFreeThreadedInitialization = new Lazy<bool>(
            () => typeof(IWorkspaceProjectContext).GetMethod("StartBatch") != null &&
                  typeof(IWorkspaceProjectContext).GetMethod("EndBatch") != null);

        /// <summary>
        /// Current AggregateWorkspaceProjectContext - accesses to this field must be done with a lock on <see cref="_gate"/>.
        /// Note that at any given time, we can have only a single non-disposed aggregate project context.
        /// Otherwise, we can end up with an invalid state of multiple workspace project contexts for the same configured project.
        /// </summary>
        private AggregateWorkspaceProjectContext _currentAggregateProjectContext;

        /// <summary>
        /// Current TargetFramework for non-cross targeting project - accesses to this field must be done with a lock on <see cref="_gate"/>.
        /// </summary>
        private string _currentTargetFramework;


        [ImportingConstructor]
        public LanguageServiceHost(IUnconfiguredProjectCommonServices commonServices,
                                   Lazy<IProjectContextProvider> contextProvider,
                                   [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
                                   IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
                                   IActiveProjectConfigurationRefreshService activeProjectConfigurationRefreshService,
                                   LanguageServiceHandlerManager languageServiceHandlerManager,
                                   IUnconfiguredProjectTasksService unconfiguredProjectTasksService)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            _commonServices = commonServices;
            _contextProvider = contextProvider;
            _tasksService = tasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _activeProjectConfigurationRefreshService = activeProjectConfigurationRefreshService;
            _languageServiceHandlerManager = languageServiceHandlerManager;
            _unconfiguredProjectTasksService = unconfiguredProjectTasksService;
            _evaluationSubscriptionLinks = new List<IDisposable>();
            _designTimeBuildSubscriptionLinks = new List<IDisposable>();
            _projectConfigurationsWithSubscriptions = new HashSet<ProjectConfiguration>();
        }

        public object HostSpecificErrorReporter => _currentAggregateProjectContext?.HostSpecificErrorReporter;

        public IWorkspaceProjectContext ActiveProjectContext => _currentAggregateProjectContext?.ActiveProjectContext;

        public object HostSpecificEditAndContinueService => _currentAggregateProjectContext?.ENCProjectConfig;

        [ProjectAutoLoad(completeBy: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.DotNetLanguageService)]
        private Task OnProjectFactoryCompletedAsync()
        {
            return InitializeAsync();
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            if (IsDisposing || IsDisposed)
                return Task.CompletedTask;

            _unconfiguredProjectTasksService.PrioritizedProjectLoadedInHostAsync(() =>
            {
                return UpdateProjectContextAndSubscriptionsAsync();
            }).Forget();

            return Task.CompletedTask;
        }

        public Task InitializeAsync()
        {
            return InitializeAsync(CancellationToken.None);
        }

        private async Task OnProjectChangedCoreAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e, RuleHandlerType handlerType)
        {
            if (IsDisposing || IsDisposed)
                return;

            await _tasksService.LoadedProjectAsync(async () =>
            {
                await HandleAsync(e, handlerType).ConfigureAwait(false);
            });

            // If "TargetFrameworks" property has changed, we need to refresh the project context and subscriptions.
            if (HasTargetFrameworksChanged(e))
            {
                await UpdateProjectContextAndSubscriptionsAsync().ConfigureAwait(false);
            }
        }

        private async Task UpdateProjectContextAndSubscriptionsAsync()
        {
            AggregateWorkspaceProjectContext previousProjectContext = _currentAggregateProjectContext;
            AggregateWorkspaceProjectContext newProjectContext = await UpdateProjectContextAsync().ConfigureAwait(false);

            if (previousProjectContext != newProjectContext)
            {
                // Add subscriptions for the new configured projects in the new project context.
                await AddSubscriptionsAsync(newProjectContext).ConfigureAwait(false);
            }
        }

        private Task<T> ExecuteWithinLockAsync<T>(Func<Task<T>> task)
        {
            return _gate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, task);
        }

        private Task ExecuteWithinLockAsync(Func<Task> task)
        {
            return _gate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, task);
        }

        private Task ExecuteWithinLockAsync(Action action)
        {
            return _gate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, action);
        }

        /// <summary>
        /// Ensures that <see cref="_currentAggregateProjectContext"/> is updated for the latest target frameworks from the project properties
        /// and returns this value.
        /// </summary>
        private async Task<AggregateWorkspaceProjectContext> UpdateProjectContextAsync()
        {
            // Ensure that only single thread is attempting to create a project context.
            AggregateWorkspaceProjectContext previousContextToDispose = null;
            return await ExecuteWithinLockAsync(async () =>
            {
                await _commonServices.ThreadingService.SwitchToUIThread();

                string newTargetFramework = null;
                ConfigurationGeneral projectProperties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);

                // Check if we have already computed the project context.
                if (_currentAggregateProjectContext != null)
                {
                    // For non-cross targeting projects, we can use the current project context if the TargetFramework hasn't changed.
                    // For cross-targeting projects, we need to verify that the current project context matches latest frameworks targeted by the project.
                    // If not, we create a new one and dispose the current one.

                    if (!_currentAggregateProjectContext.IsCrossTargeting)
                    {
                        newTargetFramework = (string)await projectProperties.TargetFramework.GetValueAsync().ConfigureAwait(false);
                        if (StringComparers.PropertyValues.Equals(_currentTargetFramework, newTargetFramework))
                        {
                            return _currentAggregateProjectContext;
                        }

                        // Dispose the old workspace project context for the previous target framework.
                        await DisposeAggregateProjectContextAsync(_currentAggregateProjectContext).ConfigureAwait(false);
                    }
                    else
                    {
                        // Check if the current project context is up-to-date for the current active and known project configurations.
                        ProjectConfiguration activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;
                        IImmutableSet<ProjectConfiguration> knownProjectConfigurations = await _commonServices.Project.Services.ProjectConfigurationsService.GetKnownProjectConfigurationsAsync().ConfigureAwait(false);
                        if (knownProjectConfigurations.All(c => c.IsCrossTargeting()) &&
                            _currentAggregateProjectContext.HasMatchingTargetFrameworks(activeProjectConfiguration, knownProjectConfigurations))
                        {
                            return _currentAggregateProjectContext;
                        }

                        previousContextToDispose = _currentAggregateProjectContext;
                    }
                }
                else
                {
                    newTargetFramework = (string)await projectProperties.TargetFramework.GetValueAsync().ConfigureAwait(false);
                }

                // Force refresh the CPS active project configuration (needs UI thread).
                await _commonServices.ThreadingService.SwitchToUIThread();
                await _activeProjectConfigurationRefreshService.RefreshActiveProjectConfigurationAsync().ConfigureAwait(false);

                // Create new project context.
                _currentAggregateProjectContext = await _contextProvider.Value.CreateProjectContextAsync().ConfigureAwait(false);
                _currentTargetFramework = newTargetFramework;

                // Dispose the old project context, if one exists.
                if (previousContextToDispose != null)
                {
                    await DisposeAggregateProjectContextAsync(previousContextToDispose).ConfigureAwait(false);
                }

                return _currentAggregateProjectContext;
            }).ConfigureAwait(false);
        }

        private async Task DisposeAggregateProjectContextAsync(AggregateWorkspaceProjectContext projectContext)
        {
            await _contextProvider.Value.ReleaseProjectContextAsync(projectContext).ConfigureAwait(false);

            foreach (IWorkspaceProjectContext innerContext in projectContext.DisposedInnerProjectContexts)
            {
                _languageServiceHandlerManager.OnContextReleased(innerContext);
            }
        }

        private async Task AddSubscriptionsAsync(AggregateWorkspaceProjectContext newProjectContext)
        {
            Requires.NotNull(newProjectContext, nameof(newProjectContext));

            await _commonServices.ThreadingService.SwitchToUIThread();
            await _tasksService.LoadedProjectAsync(() =>
            {
                IEnumerable<string> watchedEvaluationRules = _languageServiceHandlerManager.GetWatchedRules(RuleHandlerType.Evaluation);
                IEnumerable<string> watchedDesignTimeBuildRules = _languageServiceHandlerManager.GetWatchedRules(RuleHandlerType.DesignTimeBuild);

                foreach (ConfiguredProject configuredProject in newProjectContext.InnerConfiguredProjects)
                {
                    if (_projectConfigurationsWithSubscriptions.Contains(configuredProject.ProjectConfiguration))
                    {
                        continue;
                    }

                    _designTimeBuildSubscriptionLinks.Add(configuredProject.Services.ProjectSubscription.JointRuleSource.SourceBlock.LinkToAsyncAction(
                        e => OnProjectChangedCoreAsync(e, RuleHandlerType.DesignTimeBuild),
                        ruleNames: watchedDesignTimeBuildRules));

                    _evaluationSubscriptionLinks.Add(configuredProject.Services.ProjectSubscription.ProjectRuleSource.SourceBlock.LinkToAsyncAction(
                        e => OnProjectChangedCoreAsync(e, RuleHandlerType.Evaluation),
                        ruleNames: watchedEvaluationRules));

                    _projectConfigurationsWithSubscriptions.Add(configuredProject.ProjectConfiguration);
                }

                return Task.CompletedTask;
            });
        }

        private async Task HandleAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update, RuleHandlerType handlerType)
        {
            // We need to process the update within a lock to ensure that we do not release this context during processing.
            // TODO: Enable concurrent execution of updates themselves, i.e. two separate invocations of HandleAsync
            //       should be able to run concurrently.
            await ExecuteWithinLockAsync(async () =>
            {
                if (!WorkspaceSupportsBatchingAndFreeThreadedInitialization)
                {
                    await _commonServices.ThreadingService.SwitchToUIThread();
                }

                // Get the inner workspace project context to update for this change.
                IWorkspaceProjectContext projectContextToUpdate = _currentAggregateProjectContext.GetInnerProjectContext(update.Value.ProjectConfiguration, out bool isActiveContext);
                if (projectContextToUpdate == null)
                {
                    return;
                }

                _languageServiceHandlerManager.Handle(update, handlerType, projectContextToUpdate, isActiveContext);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Roslyn is working on supporting free-threaded initialization.
        /// in order to support a smooth transition, we decide whether to serialize
        /// based on whether a particular member that was added at the same time exists.
        /// </summary>
        /// <value><see langword="true"/> if the workspace supports batching.</value>
        internal static bool WorkspaceSupportsBatchingAndFreeThreadedInitialization
            => s_workspaceSupportsBatchingAndFreeThreadedInitialization.Value;

        private static bool HasTargetFrameworksChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            return e.Value.ProjectChanges.TryGetValue(ConfigurationGeneral.SchemaName, out IProjectChangeDescription projectChange) &&
                 projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworksProperty);
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                DisposeAndClearSubscriptions();

                await ExecuteWithinLockAsync(async () =>
                {
                    if (_currentAggregateProjectContext != null)
                    {
                        await _contextProvider.Value.ReleaseProjectContextAsync(_currentAggregateProjectContext).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }

        private void DisposeAndClearSubscriptions()
        {
            foreach (IDisposable link in _evaluationSubscriptionLinks.Concat(_designTimeBuildSubscriptionLinks))
            {
                link.Dispose();
            }

            _evaluationSubscriptionLinks.Clear();
            _designTimeBuildSubscriptionLinks.Clear();
            _projectConfigurationsWithSubscriptions.Clear();
        }
    }
}
