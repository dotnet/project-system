// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Base class for all snapshot filters.
    /// </summary>
    internal abstract class DependenciesSnapshotFilterBase : IDependenciesSnapshotFilter
    {
        public virtual void BeforeAddOrUpdate(
            IDependency dependency,
            AddDependencyContext context)
        {
            context.Accept(dependency);
        }
    }
}
