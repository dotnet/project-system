// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal interface IDependencyCrossTargetSubscriber
    {
        event EventHandler<DependencySubscriptionChangedEventArgs> DependenciesChanged;
        void InitializeSubscriber(ICrossTargetSubscriptionsHost host, IProjectSubscriptionService subscriptionService);
        void AddSubscriptions(AggregateCrossTargetProjectContext newProjectContext);
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
