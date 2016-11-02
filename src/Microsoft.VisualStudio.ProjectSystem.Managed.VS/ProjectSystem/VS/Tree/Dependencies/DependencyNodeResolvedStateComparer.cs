// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Compares IDependencyNode using default comparison (IDs) + Resolved state
    /// </summary>            
    internal class DependencyNodeResolvedStateComparer : IEqualityComparer<IDependencyNode>
    {
        public bool Equals(IDependencyNode x, IDependencyNode y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Equals(y) && x.Resolved == y.Resolved;
        }

        public int GetHashCode(IDependencyNode obj)
        {
            return obj.GetHashCode() ^ obj.Resolved.GetHashCode();
        }
    }
}
