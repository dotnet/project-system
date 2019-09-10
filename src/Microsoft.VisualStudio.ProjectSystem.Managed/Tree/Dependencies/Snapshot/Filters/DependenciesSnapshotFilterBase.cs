// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Base class for all snapshot filters.
    /// </summary>
    internal abstract class DependenciesSnapshotFilterBase : IDependenciesSnapshotFilter
    {
        public virtual void BeforeAddOrUpdate(
            ITargetFramework targetFramework,
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs,
            AddDependencyContext context)
        {
            context.Accept(dependency);
        }

        public virtual void BeforeRemove(
            ITargetFramework targetFramework,
            IDependency dependency,
            RemoveDependencyContext context)
        {
            context.Accept();
        }
    }
}
