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
        public DependencySubscriptionChangedEventArgs(DependenciesRuleChangeContext context)
        {
            ActiveTarget = context.ActiveTarget;
            Catalogs = context.Catalogs;
            Changes = context.Changes;
        }

        public ITargetFramework ActiveTarget { get; }

        public IProjectCatalogSnapshot Catalogs { get; }

        public ImmutableDictionary<ITargetFramework, IDependenciesChanges> Changes { get; }

        public bool AnyChanges => Changes.Count != 0;
    }
}
