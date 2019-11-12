// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
