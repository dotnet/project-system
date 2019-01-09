// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    [Export(typeof(IDependencyCrossTargetSubscriber))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependencySharedProjectsSubscriber : OnceInitializedOnceDisposed, IDependencyCrossTargetSubscriber
    {
        private readonly List<IDisposable> _subscriptionLinks = new List<IDisposable>();
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IDependenciesSnapshotProvider _dependenciesSnapshotProvider;
        private ICrossTargetSubscriptionsHost _host;

        [ImportingConstructor]
        public DependencySharedProjectsSubscriber(
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService tasksService,
            IDependenciesSnapshotProvider dependenciesSnapshotProvider)
            : base(synchronousDisposal: true)
        {
            _tasksService = tasksService;
            _dependenciesSnapshotProvider = dependenciesSnapshotProvider;
        }

        public void InitializeSubscriber(ICrossTargetSubscriptionsHost host, IProjectSubscriptionService subscriptionService)
        {
            _host = host;

            SubscribeToConfiguredProject(subscriptionService);
        }

        public void AddSubscriptions(AggregateCrossTargetProjectContext projectContext)
        {
            Requires.NotNull(projectContext, nameof(projectContext));

            foreach (ConfiguredProject configuredProject in projectContext.InnerConfiguredProjects)
            {
                SubscribeToConfiguredProject(configuredProject.Services.ProjectSubscription);
            }
        }

        public void ReleaseSubscriptions()
        {
            foreach (IDisposable link in _subscriptionLinks)
            {
                link.Dispose();
            }

            _subscriptionLinks.Clear();
        }

        private void SubscribeToConfiguredProject(IProjectSubscriptionService subscriptionService)
        {
            // Use an intermediate buffer block for project rule data to allow subsequent blocks
            // to only observe specific rule name(s).

            var intermediateBlock =
                new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    new ExecutionDataflowBlockOptions()
                    {
                        NameFormat = "Dependencies Shared Projects Input: {1}"
                    });

            _subscriptionLinks.Add(
                subscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    intermediateBlock,
                    ruleNames: ConfigurationGeneral.SchemaName,
                    suppressVersionOnlyUpdates: false,
                    linkOptions: DataflowOption.PropagateCompletion));

            ITargetBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSharedFoldersSnapshot, IProjectCatalogSnapshot>>> actionBlock =
                DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSharedFoldersSnapshot, IProjectCatalogSnapshot>>>(
                    e => OnProjectChangedAsync(e.Value),
                    new ExecutionDataflowBlockOptions()
                    {
                        NameFormat = "Dependencies Shared Projects Input: {1}"
                    });

            _subscriptionLinks.Add(ProjectDataSources.SyncLinkTo(
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

            EnsureInitialized();

            await _tasksService.LoadedProjectAsync(() =>
            {
                _tasksService.UnloadCancellationToken.ThrowIfCancellationRequested();

                return HandleAsync(e);
            });
        }

        private async Task HandleAsync(Tuple<IProjectSubscriptionUpdate, IProjectSharedFoldersSnapshot, IProjectCatalogSnapshot> e)
        {
            AggregateCrossTargetProjectContext currentAggregateContext = await _host.GetCurrentAggregateProjectContext();
            if (currentAggregateContext == null)
            {
                return;
            }

            IProjectSubscriptionUpdate projectUpdate = e.Item1;
            IProjectSharedFoldersSnapshot sharedProjectsUpdate = e.Item2;
            IProjectCatalogSnapshot catalogs = e.Item3;

            // Get the target framework to update for this change.
            ITargetFramework targetFrameworkToUpdate = currentAggregateContext.GetProjectFramework(projectUpdate.ProjectConfiguration);

            if (targetFrameworkToUpdate == null)
            {
                return;
            }

            var changesBuilder = new CrossTargetDependenciesChangesBuilder();

            ProcessSharedProjectsUpdates(sharedProjectsUpdate, targetFrameworkToUpdate, changesBuilder);

            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes = changesBuilder.TryBuildChanges();

            if (changes != null)
            {
                DependenciesChanged?.Invoke(
                    this,
                    new DependencySubscriptionChangedEventArgs(
                        currentAggregateContext.ActiveTargetFramework,
                        catalogs,
                        changes));
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
            if (!snapshot.Targets.TryGetValue(targetFramework, out ITargetedDependenciesSnapshot targetedSnapshot))
            {
                return;
            }

            IEnumerable<string> sharedFolderProjectPaths = sharedFolders.Value.Select(sf => sf.ProjectPath);
            var currentSharedImportNodes = targetedSnapshot.TopLevelDependencies
                .Where(x => x.Flags.Contains(DependencyTreeFlags.SharedProjectFlags))
                .ToList();
            IEnumerable<string> currentSharedImportNodePaths = currentSharedImportNodes.Select(x => x.Path);

            // process added nodes
            IEnumerable<string> addedSharedImportPaths = sharedFolderProjectPaths.Except(currentSharedImportNodePaths);
            foreach (string addedSharedImportPath in addedSharedImportPaths)
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
            IEnumerable<string> removedSharedImportPaths = currentSharedImportNodePaths.Except(sharedFolderProjectPaths);
            foreach (string removedSharedImportPath in removedSharedImportPaths)
            {
                bool exists = currentSharedImportNodes.Any(node => PathHelper.IsSamePath(node.Path, removedSharedImportPath));

                if (exists)
                {
                    changesBuilder.Removed(
                        targetFramework,
                        ProjectRuleHandler.ProviderTypeString,
                        dependencyId: removedSharedImportPath);
                }
            }
        }

        public event EventHandler<DependencySubscriptionChangedEventArgs> DependenciesChanged;

        protected override void Initialize()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseSubscriptions();
            }
        }
    }
}
