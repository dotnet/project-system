// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Caches and reuses <see cref="DependencyIconSet"/> instances.
    /// In practice dependencies use a relatively small number of distinct icon
    /// sets; we can save considerable amounts of memory by ensuring that the same
    /// logical sets are represented with the same instances.
    /// </summary>
    internal sealed class DependencyIconSetCache
    {
        private readonly ConcurrentDictionary<DependencyIconSet, DependencyIconSet> _iconSets = new ConcurrentDictionary<DependencyIconSet, DependencyIconSet>();

        public DependencyIconSet GetOrAddIconSet(DependencyIconSet iconSet)
        {
            return _iconSets.GetOrAdd(iconSet, set => set);
        }

        public DependencyIconSet GetOrAddIconSet(ImageMoniker icon, ImageMoniker expandedIcon, ImageMoniker unresolvedIcon, ImageMoniker unresolvedExpandedIcon)
        {
            return GetOrAddIconSet(new DependencyIconSet(icon, expandedIcon, unresolvedIcon, unresolvedExpandedIcon));
        }
    }
}
