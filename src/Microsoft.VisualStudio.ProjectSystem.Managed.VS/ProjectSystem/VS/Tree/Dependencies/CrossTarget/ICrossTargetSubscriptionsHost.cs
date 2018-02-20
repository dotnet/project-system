// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal interface ICrossTargetSubscriptionsHost
    {
        Task<AggregateCrossTargetProjectContext> GetCurrentAggregateProjectContext();
        Task<ConfiguredProject> GetConfiguredProject(ITargetFramework target);
    }
}
