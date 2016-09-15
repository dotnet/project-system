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
        private readonly Lazy<IProjectContextProvider> _contextProvider;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private IWorkspaceProjectContext _projectContext;
        private IDisposable _evaluationSubscriptionLink;
        private IDisposable _designTimeBuildSubscriptionLink;

        [ImportingConstructor]
        public LanguageServiceHost(IUnconfiguredProjectCommonServices commonServices,
                                   Lazy<IProjectContextProvider> contextProvider,
                                   [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
                                   IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)

            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(contextProvider, nameof(contextProvider));
            Requires.NotNull(tasksService, nameof(tasksService));
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));

            _commonServices = commonServices;
            _contextProvider = contextProvider;
            _tasksService = tasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;

            Handlers = new OrderPrecedenceImportCollection<ILanguageServiceRuleHandler>(projectCapabilityCheckProvider: commonServices.Project);
        }

        public object HostSpecificErrorReporter
        {
            // IWorkspaceProjectContext implements the VS-only interface IVsLanguageServiceBuildErrorReporter2
            get { return _projectContext; }
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
                  new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChanged(e, RuleHandlerType.DesignTimeBuild)),
                  ruleNames: watchedDesignTimeBuildRules.Union(watchedEvaluationRules), suppressVersionOnlyUpdates: true);

                _evaluationSubscriptionLink = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChanged(e, RuleHandlerType.Evaluation)),
                    ruleNames: watchedEvaluationRules, suppressVersionOnlyUpdates: true);
            }

            return Task.CompletedTask;
        }

        protected async override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // Don't initialize if we're unloading
            _tasksService.UnloadCancellationToken.ThrowIfCancellationRequested();

            _projectContext = await _contextProvider.Value.CreateProjectContextAsync()
                                                          .ConfigureAwait(false);

            foreach (var handler in Handlers)
            {
                handler.Value.SetContext(_projectContext);
            }
        }

        private async Task OnProjectChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> e, RuleHandlerType handlerType)
        {
            if (IsDisposing || IsDisposed)
                return;

            await InitializeAsync().ConfigureAwait(false);

            // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
            await _commonServices.ThreadingService.SwitchToUIThread();

            using (_tasksService.LoadedProject())
            {
                await HandleAsync(e, handlerType).ConfigureAwait(false);
            }
        }

        private async Task HandleAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e, RuleHandlerType handlerType)
        {
            var handlers = Handlers.Select(h => h.Value)
                                   .Where(h => h.HandlerType == handlerType);

            foreach (var handler in handlers)
            {
                IProjectChangeDescription projectChange = e.Value.ProjectChanges[handler.RuleName];
                if (projectChange.Difference.AnyChanges)
                {
                    await handler.HandleAsync(e, projectChange)
                                 .ConfigureAwait(false);
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

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                _evaluationSubscriptionLink?.Dispose();
                _designTimeBuildSubscriptionLink?.Dispose();

                var projectContext = _projectContext;
                if (projectContext != null)
                {
                    _projectContext = null;
                    await _contextProvider.Value.ReleaseProjectContextAsync(projectContext)
                                                .ConfigureAwait(false);
                }
            }
        }
    }
}
