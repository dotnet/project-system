// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
    internal sealed class ProjectRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "ProjectDependency";

        private static readonly DependencyGroupModel s_groupModel = new DependencyGroupModel(
            ProviderTypeString,
            Resources.ProjectsNodeName,
            new DependencyIconSet(
                icon: KnownMonikers.Application,
                expandedIcon: KnownMonikers.Application,
                unresolvedIcon: ManagedImageMonikers.ApplicationWarning,
                unresolvedExpandedIcon: ManagedImageMonikers.ApplicationWarning),
            DependencyTreeFlags.ProjectDependencyGroup);

        public override string ProviderType => ProviderTypeString;

        public override ImageMoniker ImplicitIcon => ManagedImageMonikers.ApplicationPrivate;

        [ImportingConstructor]
        public ProjectRuleHandler(
            IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider,
            DependenciesSnapshotProvider snapshotProvider,
            IUnconfiguredProjectCommonServices commonServices,
            [Import(AllowDefault = true)] ISolutionService? solutionService)
            : base(ProjectReference.SchemaName, ResolvedProjectReference.SchemaName)
        {
            aggregateSnapshotProvider.SnapshotChanged += OnAggregateSnapshotChanged;
            aggregateSnapshotProvider.SnapshotProviderUnloading += OnAggregateSnapshotProviderUnloading;

            // Unregister event handlers when the project unloads
            commonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloading;

            return;

            Task OnUnconfiguredProjectUnloading(object? sender, EventArgs e)
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
                // Only notify if the solution is not closing.
                // If it is closing, notifying would result in pointless updates.
                // If the solution service is unavailable, always update.
                if (solutionService?.IsSolutionClosing != true)
                {
                    OnOtherProjectDependenciesChanged(snapshotProvider.CurrentSnapshot, e.SnapshotProvider.CurrentSnapshot, shouldBeResolved: false, e.Token);
                }
            }
        }

        public override IDependencyModel CreateRootDependencyNode() => s_groupModel;

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

        /// <summary>
        /// When some other project's snapshot changed we need to check if our snapshot has a project
        /// reference upon the changed project. If it does, we need to refresh that dependency to 
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
            DependenciesSnapshot thisProjectSnapshot,
            DependenciesSnapshot otherProjectSnapshot,
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

            foreach ((ITargetFramework _, TargetedDependenciesSnapshot targetedDependencies) in thisProjectSnapshot.DependenciesByTargetFramework)
            {
                foreach ((_, IDependency dependency) in targetedDependencies.DependencyById)
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
                    dependency.BrowseObjectProperties);

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
                        changes.TryBuildChanges()!,
                        token));
            }
        }
    }
}
