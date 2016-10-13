// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using NuGet.SolutionRestoreManager;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    using TIdentityDictionary = IImmutableDictionary<NamedIdentity, IComparable>;

    internal class NuGetRestorer : OnceInitializedOnceDisposedAsync
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IVsSolutionRestoreService _solutionRestoreService;
        private readonly ActiveConfiguredProjectsIgnoringTargetFrameworkProvider _activeConfiguredProjectsProvider;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private IDisposable _evaluationSubscriptionLink;
        private IDisposable _targetFrameworkSubscriptionLink;

        private static ImmutableHashSet<string> _targetFrameworkWatchedRules = Empty.OrdinalIgnoreCaseStringSet
            .Add(ConfigurationGeneral.SchemaName);

        private static ImmutableHashSet<string> _evaluationWatchedRules = Empty.OrdinalIgnoreCaseStringSet
            .Add(ConfigurationGeneral.SchemaName)
            .Add(ProjectReference.SchemaName)
            .Add(PackageReference.SchemaName);

        [ImportingConstructor]
        public NuGetRestorer(
            IUnconfiguredProjectVsServices projectVsServices,
            IVsSolutionRestoreService solutionRestoreService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            ActiveConfiguredProjectsIgnoringTargetFrameworkProvider activeConfiguredProjectsProvider) 
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            _solutionRestoreService = solutionRestoreService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _activeConfiguredProjectsProvider = activeConfiguredProjectsProvider;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        internal Task OnProjectFactoryCompletedAsync()
        {
            // set up a subscription to listen for target framework changes
            var target = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChangedAsync(e));
            _targetFrameworkSubscriptionLink = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                target: target,
                ruleNames: _targetFrameworkWatchedRules,
                initialDataAsNew: false, // only reset on subsequent changes
                suppressVersionOnlyUpdates: true);

            return Task.CompletedTask;
        }

        private async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
        {
            if (IsDisposing || IsDisposed)
                return;

            await InitializeAsync().ConfigureAwait(false);

            // when TargetFrameworks or TargetFrameworkMoniker changes, reset subscriptions so that
            // any new configured projects are picked up
            if (HasTargetFrameworkChanged(update))
            {
                await ResetSubscriptions().ConfigureAwait(false);
            }
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {            
            await ResetSubscriptions().ConfigureAwait(false);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _evaluationSubscriptionLink?.Dispose();
            _targetFrameworkSubscriptionLink?.Dispose();
            return Task.CompletedTask;
        }

        private async Task ResetSubscriptions()
        {
            _evaluationSubscriptionLink?.Dispose();

            var currentProjects = await _activeConfiguredProjectsProvider.GetActiveConfiguredProjectsAsync().ConfigureAwait(false);

            if (currentProjects.Any())
            {
                var sourceLinkOptions = new StandardRuleDataflowLinkOptions
                {
                    RuleNames = _evaluationWatchedRules,
                    PropagateCompletion = true
                };

                var sourceBlocks = currentProjects.Select(
                    cp => cp.Services.ProjectSubscription.ProjectRuleSource.SourceBlock.SyncLinkOptions<IProjectValueVersions>(sourceLinkOptions));

                var target = new ActionBlock<Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary>>(ProjectPropertyChangedAsync);

                var targetLinkOptions = new DataflowLinkOptions { PropagateCompletion = true };

                _evaluationSubscriptionLink = ProjectDataSources.SyncLinkTo(sourceBlocks.ToImmutableList(), target, targetLinkOptions);
            }
        }

        private async Task ProjectPropertyChangedAsync(Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary> sources)
        {
            IVsProjectRestoreInfo projectRestoreInfo = ProjectRestoreInfoBuilder.Build(sources.Item1);

            if (projectRestoreInfo != null)
            {
                await _solutionRestoreService
                    .NominateProjectAsync(_projectVsServices.Project.FullPath, projectRestoreInfo, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        private static bool HasTargetFrameworkChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
        {
            IProjectChangeDescription projectChange;
            if (update.Value.ProjectChanges.TryGetValue(ConfigurationGeneral.SchemaName, out projectChange))
            {
                var changedProperties = projectChange.Difference.ChangedProperties;
                return changedProperties.Contains(ConfigurationGeneral.TargetFrameworksProperty)
                    || changedProperties.Contains(ConfigurationGeneral.TargetFrameworkMonikerProperty);
            }
            return false;
        }
    }
}
