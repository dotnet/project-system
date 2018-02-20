// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract,
            typeof(ICrossTargetRuleHandler<DependenciesRuleChangeContext>))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class ProjectRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "ProjectDependency";

        protected override string UnresolvedRuleName { get; } = ProjectReference.SchemaName;
        protected override string ResolvedRuleName { get; } = ResolvedProjectReference.SchemaName;
        public override string ProviderType { get; } = ProviderTypeString;

        [ImportingConstructor]
        public ProjectRuleHandler(IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider,
                                  IDependenciesSnapshotProvider snapshotProvider,
                                  IUnconfiguredProjectCommonServices commonServices)
        {
            AggregateSnapshotProvider = aggregateSnapshotProvider;
            SnapshotProvider = snapshotProvider;
            CommonServices = commonServices;

            AggregateSnapshotProvider.SnapshotChanged += OnSnapshotChanged;
            AggregateSnapshotProvider.SnapshotProviderUnloading += OnSnapshotProviderUnloading;

            CommonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloading;
        }

        private IUnconfiguredProjectCommonServices CommonServices { get; }
        private IDependenciesSnapshotProvider SnapshotProvider { get; }
        private IAggregateDependenciesSnapshotProvider AggregateSnapshotProvider { get; }

        public override IDependencyModel CreateRootDependencyNode()
        {
            return new SubTreeRootDependencyModel(
                ProviderType,
                VSResources.ProjectsNodeName,
                KnownMonikers.Application,
                ManagedImageMonikers.ApplicationWarning,
                DependencyTreeFlags.ProjectSubTreeRootNodeFlags);
        }

        protected override IDependencyModel CreateDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new ProjectDependencyModel(
                providerType,
                path,
                originalItemSpec,
                DependencyTreeFlags.ProjectNodeFlags,
                resolved,
                isImplicit,
                properties);
        }

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.ApplicationPrivate;
        }

        private void OnSnapshotChanged(object sender, SnapshotChangedEventArgs e)
        {
            OnOtherProjectDependenciesChanged(e.Snapshot, shouldBeResolved: true);
        }

        private void OnSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
        {
            OnOtherProjectDependenciesChanged(e.SnapshotProvider.CurrentSnapshot, shouldBeResolved: false);
        }

        /// <summary>
        /// When some other project's snapshot changed we need to check if our snapshot has a top level
        /// dependency on changed project. If it does we need to refresh those top level dependencies to 
        /// reflect changes.
        /// </summary>
        /// <param name="otherProjectSnapshot"></param>
        /// <param name="shouldBeResolved">
        /// Specifies if top-level project dependencies resolved status. When other project just had it's dependencies
        /// changed, it is resolved=true (we check target's support when we add projec dependencies). However when 
        /// other project is unloaded, we should mark top-level dependencies as unresolved.
        /// </param>
        private void OnOtherProjectDependenciesChanged(IDependenciesSnapshot otherProjectSnapshot, bool shouldBeResolved)
        {
            var projectSnapshot = SnapshotProvider.CurrentSnapshot;

            if (otherProjectSnapshot == null || projectSnapshot == null || projectSnapshot.Equals(otherProjectSnapshot))
            {
                // if any of the snapshots is not provided or this is the same project - skip
                return;
            }

            var otherProjectPath = otherProjectSnapshot.ProjectPath;

            var dependencyThatNeedChange = new List<IDependency>();
            foreach (var target in projectSnapshot.Targets)
            {
                foreach (var dependency in target.Value.TopLevelDependencies)
                {
                    // We're only interested in project dependencies
                    if (!StringComparers.DependencyProviderTypes.Equals(dependency.ProviderType, ProviderTypeString))
                        continue;

                    if (!StringComparers.Paths.Equals(otherProjectPath, dependency.FullPath))
                        continue;

                    dependencyThatNeedChange.Add(dependency);
                    break;
                }
            }

            if (dependencyThatNeedChange.Count == 0)
            {
                // we don't have dependency on updated project
                return;
            }

            foreach (var dependency in dependencyThatNeedChange)
            {
                var model = CreateDependencyModel(
                                ProviderType,
                                dependency.Path,
                                dependency.OriginalItemSpec,
                                shouldBeResolved,
                                dependency.Implicit,
                                dependency.Properties);

                var changes = new DependenciesChanges();

                // avoid unnecessary removing since, add would upgrade dependency in snapshot anyway,
                // but remove would require removing item from the tree instead of in-place upgrade.
                if (!shouldBeResolved)
                {
                    changes.IncludeRemovedChange(model);
                }

                changes.IncludeAddedChange(model);

                FireDependenciesChanged(
                    new DependenciesChangedEventArgs(
                        this,
                        dependency.TargetFramework.FullName,
                        changes,
                        catalogs: null,
                        dataSourceVersions: null));
            }
        }

        private Task OnUnconfiguredProjectUnloading(object sender, EventArgs args)
        {
            CommonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloading;
            AggregateSnapshotProvider.SnapshotChanged -= OnSnapshotChanged;
            AggregateSnapshotProvider.SnapshotProviderUnloading -= OnSnapshotProviderUnloading;

            return Task.CompletedTask;
        }
    }
}
