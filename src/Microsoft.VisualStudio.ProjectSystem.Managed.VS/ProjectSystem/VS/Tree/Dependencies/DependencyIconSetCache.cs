// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Threading;

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
        private ImmutableHashSet<DependencyIconSet> _iconSets = ImmutableHashSet<DependencyIconSet>.Empty;

        public DependencyIconSet GetOrAddIconSet(DependencyIconSet iconSet)
        {
            if (ThreadingTools.ApplyChangeOptimistically(ref _iconSets, iconSets => iconSets.Add(iconSet)))
            {
                // The cache did not already contain an equivalent icon set; use the one passed in.
                return iconSet;
            }
            else
            {
                // The cache already has an equivalent icon set; retrieve and return that one.
                _iconSets.TryGetValue(iconSet, out DependencyIconSet existingIconSet);
                return existingIconSet;
            }
        }

        public DependencyIconSet GetOrAddIconSet(ImageMoniker icon, ImageMoniker expandedIcon, ImageMoniker unresolvedIcon, ImageMoniker unresolvedExpandedIcon)
        {
            return GetOrAddIconSet(new DependencyIconSet(icon, expandedIcon, unresolvedIcon, unresolvedExpandedIcon));
        }
    }
}
