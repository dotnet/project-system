// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
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

            // TODO unsubscribe
            AggregateSnapshotProvider.SnapshotChanged += OnSnapshotChanged;
            AggregateSnapshotProvider.SnapshotProviderUnloading += OnSnapshotProviderUnloading;
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

        
        private void OnSnapshotChanged(object sender, SnapshotChangedEventArgs e)
        {
            OnOtherProjectDependenciesChanged(e.Snapshot, shouldBeResolved: true);
        }

        private void OnSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
        {
            OnOtherProjectDependenciesChanged(e.SnapshotProvider.CurrentSnapshot, shouldBeResolved: false);
        }

        private void OnOtherProjectDependenciesChanged(IDependenciesSnapshot snapshot, bool shouldBeResolved)
        { 
            if (snapshot == null)
            {
                return;
            }

            var projectPath = CommonServices.Project.FullPath;

            var dependencyThatNeedChange = new List<IDependency>();
            foreach(var target in snapshot.Targets)
            {
                foreach (var dependency in target.Value.TopLevelDependencies)
                {
                    if (snapshot.ProjectPath.Equals(dependency.GetActualPath(projectPath)))
                    {
                        dependencyThatNeedChange.Add(dependency);
                        break;
                    }
                }
            }

            if (dependencyThatNeedChange.Count == 0)
            {
                // we don't have dependencies for updated project
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
                changes.IncludeRemovedChange(model);
                changes.IncludeAddedChange(model);

                FireDependenciesChanged(
                    new DependenciesChangedEventArgs(
                        this, 
                        dependency.Snapshot.TargetFramework.Moniker, 
                        changes, 
                        catalogs:null, 
                        dataSourceVersions:null));
            }
        }
    }
}
