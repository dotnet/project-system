// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract,
            typeof(IDependenciesRuleHandler))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class ProjectRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "ProjectDependency";

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.Application,
            expandedIcon: KnownMonikers.Application,
            unresolvedIcon: ManagedImageMonikers.ApplicationWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.ApplicationWarning);

        private static readonly SubTreeRootDependencyModel s_rootModel = new SubTreeRootDependencyModel(
            ProviderTypeString,
            VSResources.ProjectsNodeName,
            s_iconSet,
            DependencyTreeFlags.ProjectSubTreeRootNodeFlags);

        public override string ProviderType => ProviderTypeString;

        [ImportingConstructor]
        public ProjectRuleHandler(
            IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider,
            IDependenciesSnapshotProvider snapshotProvider,
            IUnconfiguredProjectCommonServices commonServices)
            : base(ProjectReference.SchemaName, ResolvedProjectReference.SchemaName)
        {
            aggregateSnapshotProvider.SnapshotChanged += OnAggregateSnapshotChanged;
            aggregateSnapshotProvider.SnapshotProviderUnloading += OnAggregateSnapshotProviderUnloading;

            // Unregister event handlers when the project unloads
            commonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloading;

            return;

            Task OnUnconfiguredProjectUnloading(object sender, EventArgs e)
            {
                commonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloading;
                aggregateSnapshotProvider.SnapshotChanged -= OnAggregateSnapshotChanged;
                aggregateSnapshotProvider.SnapshotProviderUnloading -= OnAggregateSnapshotProviderUnloading;

                return Task.CompletedTask;
            }

            void OnAggregateSnapshotChanged(object sender, SnapshotChangedEventArgs e)
            {
                OnOtherProjectDependenciesChanged(snapshotProvider.CurrentSnapshot, e.Snapshot, shouldBeResolved: true, e.Token);
            }

            void OnAggregateSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
            {
                OnOtherProjectDependenciesChanged(snapshotProvider.CurrentSnapshot, e.SnapshotProvider.CurrentSnapshot, shouldBeResolved: false, e.Token);
            }
        }

        public override IDependencyModel CreateRootDependencyNode() => s_rootModel;

        protected override IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new ProjectDependencyModel(
                path,
                originalItemSpec,
                resolved,
                isImplicit,
                properties);
        }

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.ApplicationPrivate;
        }

        /// <summary>
        /// When some other project's snapshot changed we need to check if our snapshot has a top level
        /// dependency on changed project. If it does we need to refresh those top level dependencies to 
        /// reflect changes.
        /// </summary>
        /// <param name="thisProjectSnapshot"></param>
        /// <param name="otherProjectSnapshot"></param>
        /// <param name="shouldBeResolved">
        /// Specifies if top-level project dependencies resolved status. When other project just had its dependencies
        /// changed, it is resolved=true (we check target's support when we add project dependencies). However when 
        /// other project is unloaded, we should mark top-level dependencies as unresolved.
        /// </param>
        /// <param name="token"></param>
        private void OnOtherProjectDependenciesChanged(
            IDependenciesSnapshot thisProjectSnapshot,
            IDependenciesSnapshot otherProjectSnapshot,
            bool shouldBeResolved,
            CancellationToken token)
        {
            if (token.IsCancellationRequested ||
                StringComparers.Paths.Equals(thisProjectSnapshot.ProjectPath, otherProjectSnapshot.ProjectPath))
            {
                // if any of the snapshots is not provided or this is the same project - skip
                return;
            }

            string otherProjectPath = otherProjectSnapshot.ProjectPath;

            foreach ((ITargetFramework _, ITargetedDependenciesSnapshot targetedDependencies) in thisProjectSnapshot.Targets)
            {
                foreach (IDependency dependency in targetedDependencies.TopLevelDependencies)
                {
                    if (token.IsCancellationRequested)
                        return;

                    // We're only interested in project dependencies
                    if (!StringComparers.DependencyProviderTypes.Equals(dependency.ProviderType, ProviderTypeString))
                        continue;

                    if (!StringComparers.Paths.Equals(otherProjectPath, dependency.FullPath))
                        continue;

                    RaiseChangeEvent(dependency);
                    break;
                }
            }

            return;

            void RaiseChangeEvent(IDependency dependency)
            {
                IDependencyModel model = CreateDependencyModel(
                    dependency.Path,
                    dependency.OriginalItemSpec,
                    shouldBeResolved,
                    dependency.Implicit,
                    dependency.Properties);

                var changes = new DependenciesChangesBuilder();

                // avoid unnecessary removing since, add would upgrade dependency in snapshot anyway,
                // but remove would require removing item from the tree instead of in-place upgrade.
                if (!shouldBeResolved)
                {
                    changes.Removed(ProviderTypeString, model.Id);
                }

                changes.Added(model);

                FireDependenciesChanged(
                    new DependenciesChangedEventArgs(
                        this,
                        dependency.TargetFramework.FullName,
                        changes.Build(),
                        token));
            }
        }
    }
}
