// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Base class for all snapshot filters.
    /// </summary>
    internal abstract class DependenciesSnapshotFilterBase : IDependenciesSnapshotFilter
    {
        public virtual void BeforeAddOrUpdate(
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs,
            AddDependencyContext context)
        {
            context.Accept(dependency);
        }

        public virtual void BeforeRemove(
            IDependency dependency,
            RemoveDependencyContext context)
        {
            context.Accept();
        }
    }
}
