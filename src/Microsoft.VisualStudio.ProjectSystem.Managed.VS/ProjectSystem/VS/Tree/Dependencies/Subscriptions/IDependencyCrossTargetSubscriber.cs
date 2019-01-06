// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    /// <summary>
    ///     Implementations subscribe to project data sources, and produce project dependency data.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Instances are imported into a <see cref="ICrossTargetSubscriptionsHost"/>.
    /// </para>
    /// <para>
    ///     That host will call <see cref="InitializeSubscriber"/> once, then call <see cref="AddSubscriptions"/>
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
    internal interface IDependencyCrossTargetSubscriber
    {
        /// <summary>
        ///     Raised whenever this subscriber has new data about project dependencies.
        /// </summary>
        event EventHandler<DependencySubscriptionChangedEventArgs> DependenciesChanged;

        /// <summary>
        ///     Called once, when this subscriber is first loaded into its <paramref name="host"/>.
        /// </summary>
        /// <param name="host">The object that's hosting this subscriber.</param>
        /// <param name="subscriptionService">An object that provides access to project data.</param>
        void InitializeSubscriber(ICrossTargetSubscriptionsHost host, IProjectSubscriptionService subscriptionService);

        /// <summary>
        ///     Requests this subscriber to create subscriptions based on the target frameworks specified in <paramref name="projectContext"/>.
        /// </summary>
        void AddSubscriptions(AggregateCrossTargetProjectContext projectContext);

        /// <summary>
        ///     Requests this subscriber to release all previously created subscriptions.
        /// </summary>
        void ReleaseSubscriptions();
    }

    internal sealed class DependencySubscriptionChangedEventArgs
    {
        public DependencySubscriptionChangedEventArgs(
            ITargetFramework activeTarget,
            IProjectCatalogSnapshot catalogs,
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes)
        {
            Requires.Argument(changes.Count != 0, nameof(changes), "Must not be zero.");

            ActiveTarget = activeTarget;
            Catalogs = catalogs;
            Changes = changes;
        }

        public ITargetFramework ActiveTarget { get; }

        public IProjectCatalogSnapshot Catalogs { get; }

        public ImmutableDictionary<ITargetFramework, IDependenciesChanges> Changes { get; }
    }
}
