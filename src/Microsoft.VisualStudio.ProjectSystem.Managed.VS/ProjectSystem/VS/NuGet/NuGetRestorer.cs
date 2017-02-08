// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly ActiveConfiguredProjectsProvider _activeConfiguredProjectsProvider;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IActiveProjectConfigurationRefreshService _activeProjectConfigurationRefreshService;
        private IDisposable _designTimeBuildSubscriptionLink;
        private IDisposable _targetFrameworkSubscriptionLink;

        private const int perfPackageRestoreEnd = 7343;

        private static ImmutableHashSet<string> _targetFrameworkWatchedRules = Empty.OrdinalIgnoreCaseStringSet
            .Add(NuGetRestore.SchemaName);

        private static ImmutableHashSet<string> _designTimeBuildWatchedRules = Empty.OrdinalIgnoreCaseStringSet
            .Add(NuGetRestore.SchemaName)
            .Add(ProjectReference.SchemaName)
            .Add(PackageReference.SchemaName)
            .Add(DotNetCliToolReference.SchemaName);

        [ImportingConstructor]
        public NuGetRestorer(
            IUnconfiguredProjectVsServices projectVsServices,
            IVsSolutionRestoreService solutionRestoreService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            IActiveProjectConfigurationRefreshService activeProjectConfigurationRefreshService,
            ActiveConfiguredProjectsProvider activeConfiguredProjectsProvider) 
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            _solutionRestoreService = solutionRestoreService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _activeProjectConfigurationRefreshService = activeProjectConfigurationRefreshService;
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
            _designTimeBuildSubscriptionLink?.Dispose();
            _targetFrameworkSubscriptionLink?.Dispose();
            return Task.CompletedTask;
        }

        private async Task ResetSubscriptions()
        {
            // active configuration should be updated before resetting subscriptions
            await RefreshActiveConfigurationAsync().ConfigureAwait(false);

            _designTimeBuildSubscriptionLink?.Dispose();

            var currentProjects = await _activeConfiguredProjectsProvider.GetActiveConfiguredProjectsAsync().ConfigureAwait(false);

            if (currentProjects.Any())
            {
                var sourceLinkOptions = new StandardRuleDataflowLinkOptions
                {
                    RuleNames = _designTimeBuildWatchedRules,
                    PropagateCompletion = true
                };

                var sourceBlocks = currentProjects.Select(
                    cp => cp.Services.ProjectSubscription.JointRuleSource.SourceBlock.SyncLinkOptions<IProjectValueVersions>(sourceLinkOptions));

                var target = new ActionBlock<Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary>>(ProjectPropertyChangedAsync);

                var targetLinkOptions = new DataflowLinkOptions { PropagateCompletion = true };

                _designTimeBuildSubscriptionLink = ProjectDataSources.SyncLinkTo(sourceBlocks.ToImmutableList(), target, targetLinkOptions);
            }
        }

        private async Task RefreshActiveConfigurationAsync()
        {
            // Force refresh the CPS active project configuration (needs UI thread).
            await _projectVsServices.ThreadingService.SwitchToUIThread();
            await _activeProjectConfigurationRefreshService.RefreshActiveProjectConfigurationAsync().ConfigureAwait(false);
        }

        private Task ProjectPropertyChangedAsync(Tuple<ImmutableList<IProjectValueVersions>, TIdentityDictionary> sources)
        {
            IVsProjectRestoreInfo projectRestoreInfo = ProjectRestoreInfoBuilder.Build(sources.Item1, _projectVsServices.Project);

            if (projectRestoreInfo != null)
            {
                _projectVsServices.Project.Services.ProjectAsynchronousTasks
                    .RegisterAsyncTask(JoinableFactory.RunAsync(async () =>
                    {
                        await _solutionRestoreService
                               .NominateProjectAsync(_projectVsServices.Project.FullPath, projectRestoreInfo,
                                    _projectVsServices.Project.Services.ProjectAsynchronousTasks.UnloadCancellationToken)
                               .ConfigureAwait(false);

                        Microsoft.Internal.Performance.CodeMarkers.Instance.CodeMarker(perfPackageRestoreEnd);

                    }),
                    ProjectCriticalOperation.Build | ProjectCriticalOperation.Unload | ProjectCriticalOperation.Rename,
                    registerFaultHandler: true);
            }

            return Task.CompletedTask;
        }

        private static bool HasTargetFrameworkChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
        {
            if (update.Value.ProjectChanges.TryGetValue(NuGetRestore.SchemaName, out IProjectChangeDescription projectChange))
            {
                var changedProperties = projectChange.Difference.ChangedProperties;
                return changedProperties.Contains(NuGetRestore.TargetFrameworksProperty)
                    || changedProperties.Contains(NuGetRestore.TargetFrameworkProperty)
                    || changedProperties.Contains(NuGetRestore.TargetFrameworkMonikerProperty);
            }
            return false;
        }
    }
}
