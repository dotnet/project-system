// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;

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
        private ImmutableHashSet<DependencyIconSet> _iconSets = ImmutableHashSet<DependencyIconSet>.Empty;

        public DependencyIconSet GetOrAddIconSet(DependencyIconSet iconSet)
        {
            return GetOrAdd(ref _iconSets, iconSet);
        }

        public DependencyIconSet GetOrAddIconSet(ImageMoniker icon, ImageMoniker expandedIcon, ImageMoniker unresolvedIcon, ImageMoniker unresolvedExpandedIcon)
        {
            return GetOrAddIconSet(new DependencyIconSet(icon, expandedIcon, unresolvedIcon, unresolvedExpandedIcon));
        }

        private static T GetOrAdd<T>(ref ImmutableHashSet<T> location, T value)
        {
            ImmutableHashSet<T> prior = Volatile.Read(ref location);

            while (true)
            {
                if (prior.TryGetValue(value, out T existingValue))
                {
                    // The value already exists in the set. Return it.
                    return existingValue;
                }

                // Add a value to the set. This will succeed as we've just seen that value is not in prior.
                ImmutableHashSet<T> updated = prior.Add(value);

                // Attempt to update the field optimistically.
                ImmutableHashSet<T> original = Interlocked.CompareExchange(ref location, updated, prior);

                if (ReferenceEquals(original, prior))
                {
                    // The update was successful; return the added icon set.
                    return value;
                }

                // Optimistic update failed, so loop around and try again.
                prior = original;
            }
        }
    }
}
