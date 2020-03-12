// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Compares equality of <see cref="IDependency"/> instances based on <see cref="IDependency.Id"/>.
    /// </summary>
    internal sealed class DependencyIdComparer : IEqualityComparer<IDependency>
    {
        public static IEqualityComparer<IDependency> Instance { get; } = new DependencyIdComparer();

        public bool Equals(IDependency? x, IDependency? y)
        {
            return StringComparers.DependencyTreeIds.Equals(x?.Id, y?.Id);
        }

        public int GetHashCode(IDependency dependency)
        {
            return StringComparers.DependencyTreeIds.GetHashCode(dependency.Id);
        }
    }
}
