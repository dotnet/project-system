// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal interface IDependencyCrossTargetSubscriber : ICrossTargetSubscriber
    {
        event EventHandler<DependencySubscriptionChangedEventArgs> DependenciesChanged;
    }

    internal class DependencySubscriptionChangedEventArgs
    {
        public DependencySubscriptionChangedEventArgs(DependenciesRuleChangeContext context)
        {
            Context = context;
        }

        public DependenciesRuleChangeContext Context { get; }
    }
}
