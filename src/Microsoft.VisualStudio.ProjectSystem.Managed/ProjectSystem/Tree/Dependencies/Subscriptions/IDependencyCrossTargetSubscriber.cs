// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions
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
            ImmutableArray<TargetFramework> targetFrameworks,
            TargetFramework activeTarget,
            TargetFramework changedTargetFramework,
            IDependenciesChanges? changes,
            IProjectCatalogSnapshot catalogs)
        {
            Requires.Argument(!targetFrameworks.IsDefaultOrEmpty, nameof(targetFrameworks), "Must not be default or empty.");

            TargetFrameworks = targetFrameworks;
            ActiveTarget = activeTarget;
            Catalogs = catalogs;
            Changes = changes;
            ChangedTargetFramework = changedTargetFramework;
        }

        public ImmutableArray<TargetFramework> TargetFrameworks { get; }

        public TargetFramework ActiveTarget { get; }

        public IProjectCatalogSnapshot Catalogs { get; }

        public IDependenciesChanges? Changes { get; }

        public TargetFramework ChangedTargetFramework { get; }
    }
}
