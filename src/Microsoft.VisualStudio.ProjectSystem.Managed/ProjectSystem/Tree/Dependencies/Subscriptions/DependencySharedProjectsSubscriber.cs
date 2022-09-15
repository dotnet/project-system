// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using EventData = System.Tuple<
    Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate,
    Microsoft.VisualStudio.ProjectSystem.IProjectSharedFoldersSnapshot,
    Microsoft.VisualStudio.ProjectSystem.Properties.IProjectCatalogSnapshot,
    Microsoft.VisualStudio.ProjectSystem.IProjectCapabilitiesSnapshot>;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions
{
    [Export(typeof(IDependencyCrossTargetSubscriber))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class DependencySharedProjectsSubscriber : DependencyRulesSubscriberBase<EventData>
    {
        private readonly DependenciesSnapshotProvider _dependenciesSnapshotProvider;

        [ImportingConstructor]
        public DependencySharedProjectsSubscriber(
            IProjectThreadingService threadingService,
            IUnconfiguredProjectTasksService tasksService,
            DependenciesSnapshotProvider dependenciesSnapshotProvider)
            : base(threadingService, tasksService)
        {
            _dependenciesSnapshotProvider = dependenciesSnapshotProvider;
        }

        protected override void SubscribeToConfiguredProject(
            ConfiguredProject configuredProject,
            IProjectSubscriptionService subscriptionService)
        {
            Subscribe(
                configuredProject,
                subscriptionService.ProjectRuleSource,
                ruleNames: new[] { ConfigurationGeneral.SchemaName },
                "Dependencies Shared Projects Input: {1}",
                blocks => ProjectDataSources.SyncLinkTo(
                    blocks.Intermediate.SyncLinkOptions(),
                    subscriptionService.SharedFoldersSource.SourceBlock.SyncLinkOptions(),
                    subscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                    configuredProject.Capabilities.SourceBlock.SyncLinkOptions(),
                    blocks.Action,
                    linkOptions: DataflowOption.PropagateCompletion));
        }

        protected override IProjectCapabilitiesSnapshot GetCapabilitiesSnapshot(EventData e) => e.Item4;
        protected override ProjectConfiguration GetProjectConfiguration(EventData e) => e.Item1.ProjectConfiguration;

        protected override void Handle(
            string projectFullPath,
            AggregateCrossTargetProjectContext currentAggregateContext,
            TargetFramework targetFrameworkToUpdate,
            EventData e,
            CancellationToken cancellationToken)
        {
            IProjectSharedFoldersSnapshot sharedProjectsUpdate = e.Item2;
            IProjectCatalogSnapshot catalogs = e.Item3;

            var changesBuilder = new DependenciesChangesBuilder();

            ProcessSharedProjectsUpdates(sharedProjectsUpdate, targetFrameworkToUpdate, changesBuilder);

            IDependenciesChanges? changes = changesBuilder.TryBuildChanges();

            if (changes is not null)
            {
                RaiseDependenciesChanged(targetFrameworkToUpdate, changes, currentAggregateContext, catalogs);
            }
        }

        private void ProcessSharedProjectsUpdates(
            IProjectSharedFoldersSnapshot sharedFolders,
            TargetFramework targetFramework,
            DependenciesChangesBuilder changesBuilder)
        {
            Requires.NotNull(sharedFolders, nameof(sharedFolders));
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.NotNull(changesBuilder, nameof(changesBuilder));

            DependenciesSnapshot snapshot = _dependenciesSnapshotProvider.CurrentSnapshot;
            if (!snapshot.DependenciesByTargetFramework.TryGetValue(targetFramework, out TargetedDependenciesSnapshot? targetedSnapshot))
            {
                return;
            }

            IEnumerable<string> sharedFolderProjectPaths = sharedFolders.Value.Select(sf => sf.ProjectPath);
            var currentSharedImportNodePaths = targetedSnapshot.Dependencies
                .Where(pair => pair.Flags.Contains(DependencyTreeFlags.SharedProjectDependency))
                .Select(pair => pair.FilePath!)
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
