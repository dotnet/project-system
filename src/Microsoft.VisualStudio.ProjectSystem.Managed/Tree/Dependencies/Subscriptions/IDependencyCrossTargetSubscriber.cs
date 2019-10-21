// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    /// <summary>
    ///     Implementations subscribe to project data sources, and produce project dependency data.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Instances are imported into a <see cref="DependenciesSnapshotProvider"/>.
    /// </para>
    /// <para>
    ///     That host will call <see cref="InitializeSubscriberAsync"/> once, then call <see cref="AddSubscriptions"/>
    ///     with details of target frameworks to subscribe to.
    /// </para>
    /// <para>
    ///     If the host's <see cref="AggregateCrossTargetProjectContext"/> changes, the host
    ///     will call <see cref="ReleaseSubscriptions"/> before calling <see cref="AddSubscriptions"/>
    ///     with the updated project context.
    /// </para>
    /// <para>
    ///     When the host is disposed, it will call <see cref="ReleaseSubscriptions"/>.
    /// </para>
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface IDependencyCrossTargetSubscriber
    {
        /// <summary>
        ///     Raised whenever this subscriber has new data about project dependencies.
        /// </summary>
        event EventHandler<DependencySubscriptionChangedEventArgs> DependenciesChanged;

        /// <summary>
        ///     Called once, when this subscriber is first loaded into its <paramref name="provider"/>.
        /// </summary>
        /// <param name="provider">The object that's hosting this subscriber.</param>
        Task InitializeSubscriberAsync(DependenciesSnapshotProvider provider);

        /// <summary>
        ///     Requests this subscriber to create subscriptions based on the target frameworks specified in <paramref name="projectContext"/>.
        /// </summary>
        /// <remarks>
        ///     The caller is responsible for synchronizing calls to this and <see cref="ReleaseSubscriptions"/>.
        /// </remarks>
        void AddSubscriptions(AggregateCrossTargetProjectContext projectContext);

        /// <summary>
        ///     Requests this subscriber to release all previously created subscriptions.
        /// </summary>
        /// <remarks>
        ///     The caller is responsible for synchronizing calls to this and <see cref="AddSubscriptions"/>.
        /// </remarks>
        void ReleaseSubscriptions();
    }

    internal sealed class DependencySubscriptionChangedEventArgs
    {
        public DependencySubscriptionChangedEventArgs(
            ImmutableArray<ITargetFramework> targetFrameworks,
            ITargetFramework activeTarget,
            ITargetFramework changedTargetFramework,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot catalogs)
        {
            Requires.Argument(!targetFrameworks.IsDefaultOrEmpty, nameof(targetFrameworks), "Must not be default or empty.");

            TargetFrameworks = targetFrameworks;
            ActiveTarget = activeTarget;
            Catalogs = catalogs;
            Changes = changes;
            ChangedTargetFramework = changedTargetFramework;
        }

        public ImmutableArray<ITargetFramework> TargetFrameworks { get; }

        public ITargetFramework ActiveTarget { get; }

        public IProjectCatalogSnapshot Catalogs { get; }

        public IDependenciesChanges Changes { get; }

        public ITargetFramework ChangedTargetFramework { get; }
    }
}
