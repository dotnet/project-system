// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

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
        public static DependencyIconSetCache Instance { get; } = new DependencyIconSetCache();

        private ImmutableDictionary<(ImageMoniker Icon, ImageMoniker ExpandedIcon, ImageMoniker UnresolvedIcon, ImageMoniker UnresolvedExpandedIcon), DependencyIconSet>
            _iconSets = ImmutableDictionary<(ImageMoniker, ImageMoniker, ImageMoniker, ImageMoniker), DependencyIconSet>.Empty.WithComparers(Comparer.Instance);

        public DependencyIconSet GetOrAddIconSet(DependencyIconSet iconSet)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref _iconSets,
                (iconSet.Icon, iconSet.ExpandedIcon, iconSet.UnresolvedIcon, iconSet.UnresolvedExpandedIcon),
                (key, arg) => arg,
                iconSet);
        }

        public DependencyIconSet GetOrAddIconSet(ImageMoniker icon, ImageMoniker expandedIcon, ImageMoniker unresolvedIcon, ImageMoniker unresolvedExpandedIcon)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref _iconSets,
                (Icon: icon, ExpandedIcon: expandedIcon, UnresolvedIcon: unresolvedIcon, UnresolvedExpandedIcon: unresolvedExpandedIcon),
                key => new DependencyIconSet(key.Icon, key.ExpandedIcon, key.UnresolvedIcon, key.UnresolvedExpandedIcon));
        }

        /// <summary>Custom equality comparer, to prevent boxing value tuples during dictionary operations.</summary>
        private sealed class Comparer : IEqualityComparer<(ImageMoniker Icon, ImageMoniker ExpandedIcon, ImageMoniker UnresolvedIcon, ImageMoniker UnresolvedExpandedIcon)>
        {
            public static Comparer Instance { get; } = new Comparer();

            public bool Equals(
                (ImageMoniker Icon, ImageMoniker ExpandedIcon, ImageMoniker UnresolvedIcon, ImageMoniker UnresolvedExpandedIcon) x,
                (ImageMoniker Icon, ImageMoniker ExpandedIcon, ImageMoniker UnresolvedIcon, ImageMoniker UnresolvedExpandedIcon) y)
            {
                return x.Icon.Id == y.Icon.Id &&
                       x.ExpandedIcon.Id == y.ExpandedIcon.Id &&
                       x.UnresolvedIcon.Id == y.UnresolvedIcon.Id &&
                       x.UnresolvedExpandedIcon.Id == y.UnresolvedExpandedIcon.Id &&
                       x.Icon.Guid == y.Icon.Guid &&
                       x.ExpandedIcon.Guid == y.ExpandedIcon.Guid &&
                       x.UnresolvedIcon.Guid == y.UnresolvedIcon.Guid &&
                       x.UnresolvedExpandedIcon.Guid == y.UnresolvedExpandedIcon.Guid;
            }

            public int GetHashCode((ImageMoniker Icon, ImageMoniker ExpandedIcon, ImageMoniker UnresolvedIcon, ImageMoniker UnresolvedExpandedIcon) obj)
            {
                int hashCode = obj.Icon.Id;
                hashCode = (hashCode * -1521134295) ^ obj.Icon.Guid.GetHashCode();
                hashCode = (hashCode * -1521134295) ^ obj.ExpandedIcon.Id;
                hashCode = (hashCode * -1521134295) ^ obj.ExpandedIcon.Guid.GetHashCode();
                hashCode = (hashCode * -1521134295) ^ obj.UnresolvedIcon.Id;
                hashCode = (hashCode * -1521134295) ^ obj.UnresolvedIcon.Guid.GetHashCode();
                hashCode = (hashCode * -1521134295) ^ obj.UnresolvedExpandedIcon.Id;
                hashCode = (hashCode * -1521134295) ^ obj.UnresolvedExpandedIcon.Guid.GetHashCode();
                return hashCode;
            }
        }
    }
}
