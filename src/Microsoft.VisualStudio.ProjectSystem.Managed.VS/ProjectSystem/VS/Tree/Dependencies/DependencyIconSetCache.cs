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
            return ImmutableInterlockedEx.GetOrAdd(ref _iconSets, iconSet);
        }

        public DependencyIconSet GetOrAddIconSet(ImageMoniker icon, ImageMoniker expandedIcon, ImageMoniker unresolvedIcon, ImageMoniker unresolvedExpandedIcon)
        {
            return GetOrAddIconSet(new DependencyIconSet(icon, expandedIcon, unresolvedIcon, unresolvedExpandedIcon));
        }
    }
}
