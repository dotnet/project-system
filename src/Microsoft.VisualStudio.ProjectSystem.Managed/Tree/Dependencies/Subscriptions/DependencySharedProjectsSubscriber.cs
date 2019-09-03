// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    [Export(typeof(IDependencyCrossTargetSubscriber))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class DependencySharedProjectsSubscriber : DependencyRulesSubscriberBase
    {
        private readonly IDependenciesSnapshotProvider _dependenciesSnapshotProvider;

        [ImportingConstructor]
        public DependencySharedProjectsSubscriber(
            IUnconfiguredProjectCommonServices commonServices,
            IUnconfiguredProjectTasksService tasksService,
            IDependenciesSnapshotProvider dependenciesSnapshotProvider)
            : base(tasksService, commonServices.ThreadingService.JoinableTaskContext)
        {
            _dependenciesSnapshotProvider = dependenciesSnapshotProvider;
        }

        protected override void AddSubscriptionsInternal(AggregateCrossTargetProjectContext projectContext)
        {
            foreach (ConfiguredProject configuredProject in projectContext.InnerConfiguredProjects)
            {
                InitializeSubscriber(configuredProject.Services.ProjectSubscription);
            }
        }

        protected override void InitializeSubscriber(IProjectSubscriptionService subscriptionService)
        {
            // Use an intermediate buffer block for project rule data to allow subsequent blocks
            // to only observe specific rule name(s).

            var intermediateBlock =
                new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    new ExecutionDataflowBlockOptions
                    {
                        NameFormat = "Dependencies Shared Projects Input: {1}"
                    });

            ITargetBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSharedFoldersSnapshot, IProjectCatalogSnapshot>>> actionBlock =
                DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSharedFoldersSnapshot, IProjectCatalogSnapshot>>>(
                    e => OnProjectChangedAsync(e.Value),
                    new ExecutionDataflowBlockOptions
                    {
                        NameFormat = "Dependencies Shared Projects Input: {1}"
                    });

            Subscriptions ??= new DisposableBag();

            Subscriptions!.Add(
                subscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    intermediateBlock,
                    ruleNames: ConfigurationGeneral.SchemaName,
                    suppressVersionOnlyUpdates: false,
                    linkOptions: DataflowOption.PropagateCompletion));

            Subscriptions.Add(ProjectDataSources.SyncLinkTo(
                intermediateBlock.SyncLinkOptions(),
                subscriptionService.SharedFoldersSource.SourceBlock.SyncLinkOptions(),
                subscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                actionBlock,
                linkOptions: DataflowOption.PropagateCompletion));
        }

        private async Task OnProjectChangedAsync(Tuple<IProjectSubscriptionUpdate, IProjectSharedFoldersSnapshot, IProjectCatalogSnapshot> e)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            await InitializeAsync();

            await TasksService.LoadedProjectAsync(() =>
            {
                return HandleAsync(e);
            });
        }

        private async Task HandleAsync(Tuple<IProjectSubscriptionUpdate, IProjectSharedFoldersSnapshot, IProjectCatalogSnapshot> e)
        {
            AggregateCrossTargetProjectContext? currentAggregateContext = await Host!.GetCurrentAggregateProjectContextAsync();
            if (currentAggregateContext == null)
            {
                return;
            }

            IProjectSubscriptionUpdate projectUpdate = e.Item1;
            IProjectSharedFoldersSnapshot sharedProjectsUpdate = e.Item2;
            IProjectCatalogSnapshot catalogs = e.Item3;

            // Get the target framework to update for this change.
            ITargetFramework? targetFrameworkToUpdate = currentAggregateContext.GetProjectFramework(projectUpdate.ProjectConfiguration);

            if (targetFrameworkToUpdate == null)
            {
                return;
            }

            var changesBuilder = new CrossTargetDependenciesChangesBuilder();

            ProcessSharedProjectsUpdates(sharedProjectsUpdate, targetFrameworkToUpdate, changesBuilder);

            ImmutableDictionary<ITargetFramework, IDependenciesChanges>? changes = changesBuilder.TryBuildChanges();

            if (changes != null)
            {
                RaiseDependenciesChanged(changes, currentAggregateContext, catalogs);
            }
        }

        private void ProcessSharedProjectsUpdates(
            IProjectSharedFoldersSnapshot sharedFolders,
            ITargetFramework targetFramework,
            CrossTargetDependenciesChangesBuilder changesBuilder)
        {
            Requires.NotNull(sharedFolders, nameof(sharedFolders));
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.NotNull(changesBuilder, nameof(changesBuilder));

            IDependenciesSnapshot snapshot = _dependenciesSnapshotProvider.CurrentSnapshot;
            if (!snapshot.DependenciesByTargetFramework.TryGetValue(targetFramework, out ITargetedDependenciesSnapshot targetedSnapshot))
            {
                return;
            }

            IEnumerable<string> sharedFolderProjectPaths = sharedFolders.Value.Select(sf => sf.ProjectPath);
            var currentSharedImportNodePaths = targetedSnapshot.TopLevelDependencies
                .Where(x => x.Flags.Contains(DependencyTreeFlags.SharedProjectFlags))
                .Select(x => x.Path)
                .ToList();

            var diff = new SetDiff<string>(currentSharedImportNodePaths, sharedFolderProjectPaths);

            // process added nodes
            foreach (string addedSharedImportPath in diff.Added)
            {
                IDependencyModel added = new SharedProjectDependencyModel(
                    addedSharedImportPath,
                    addedSharedImportPath,
                    isResolved: true,
                    isImplicit: false,
                    properties: ImmutableStringDictionary<string>.EmptyOrdinal);
                changesBuilder.Added(targetFramework, added);
            }

            // process removed nodes
            foreach (string removedSharedImportPath in diff.Removed)
            {
                bool exists = currentSharedImportNodePaths.Any(nodePath => PathHelper.IsSamePath(nodePath, removedSharedImportPath));

                if (exists)
                {
                    changesBuilder.Removed(
                        targetFramework,
                        ProjectRuleHandler.ProviderTypeString,
                        dependencyId: removedSharedImportPath);
                }
            }
        }
    }
}
