// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal interface ICrossTargetSubscriber
    {
        Task InitializeSubscriberAsync(ICrossTargetSubscriptionsHost host, IProjectSubscriptionService subscriptionService);
        Task AddSubscriptionsAsync(AggregateCrossTargetProjectContext newProjectContext);
        Task ReleaseSubscriptionsAsync();
        Task OnContextReleasedAsync(ITargetedProjectContext innerContext);
    }
}
