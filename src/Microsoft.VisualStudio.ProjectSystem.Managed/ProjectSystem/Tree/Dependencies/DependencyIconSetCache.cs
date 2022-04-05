// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    /// <summary>
    /// Caches and reuses <see cref="DependencyIconSet"/> instances.
    /// In practice dependencies use a relatively small number of distinct icon
    /// sets; we can save considerable amounts of memory by ensuring that the same
    /// logical sets are represented with the same instances.
    /// </summary>
    internal sealed class DependencyIconSetCache
    {
        public static DependencyIconSetCache Instance { get; } = new();

        private ImmutableDictionary<(ImageMoniker Icon, ImageMoniker ExpandedIcon, ImageMoniker UnresolvedIcon, ImageMoniker UnresolvedExpandedIcon, ImageMoniker ImplicitIcon, ImageMoniker ImplicitExpandedIcon), DependencyIconSet>
            _iconSets = ImmutableDictionary<(ImageMoniker, ImageMoniker, ImageMoniker, ImageMoniker, ImageMoniker ImplicitIcon, ImageMoniker ImplicitExpandedIcon), DependencyIconSet>.Empty.WithComparers(Comparer.Instance);

        public DependencyIconSet GetOrAddIconSet(DependencyIconSet iconSet)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref _iconSets,
                (iconSet.Icon, iconSet.ExpandedIcon, iconSet.UnresolvedIcon, iconSet.UnresolvedExpandedIcon, iconSet.ImplicitIcon, iconSet.ImplicitExpandedIcon),
                (key, arg) => arg,
                iconSet);
        }

        public DependencyIconSet GetOrAddIconSet(ImageMoniker icon, ImageMoniker expandedIcon, ImageMoniker unresolvedIcon, ImageMoniker unresolvedExpandedIcon, ImageMoniker implicitIcon, ImageMoniker implicitExpandedIcon)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref _iconSets,
                (Icon: icon, ExpandedIcon: expandedIcon, UnresolvedIcon: unresolvedIcon, UnresolvedExpandedIcon: unresolvedExpandedIcon, ImplicitIcon: implicitIcon, ImplicitExpandedIcon: implicitExpandedIcon),
                key => new DependencyIconSet(key.Icon, key.ExpandedIcon, key.UnresolvedIcon, key.UnresolvedExpandedIcon, key.ImplicitIcon, key.ImplicitExpandedIcon));
        }

        /// <summary>Custom equality comparer, to prevent boxing value tuples during dictionary operations.</summary>
        private sealed class Comparer : IEqualityComparer<(ImageMoniker Icon, ImageMoniker ExpandedIcon, ImageMoniker UnresolvedIcon, ImageMoniker UnresolvedExpandedIcon, ImageMoniker ImplicitIcon, ImageMoniker ImplicitExpandedIcon)>
        {
            public static Comparer Instance { get; } = new Comparer();

            public bool Equals(
                (ImageMoniker Icon, ImageMoniker ExpandedIcon, ImageMoniker UnresolvedIcon, ImageMoniker UnresolvedExpandedIcon, ImageMoniker ImplicitIcon, ImageMoniker ImplicitExpandedIcon) x,
                (ImageMoniker Icon, ImageMoniker ExpandedIcon, ImageMoniker UnresolvedIcon, ImageMoniker UnresolvedExpandedIcon, ImageMoniker ImplicitIcon, ImageMoniker ImplicitExpandedIcon) y)
            {
                return x.Icon.Id == y.Icon.Id &&
                       x.ExpandedIcon.Id == y.ExpandedIcon.Id &&
                       x.UnresolvedIcon.Id == y.UnresolvedIcon.Id &&
                       x.UnresolvedExpandedIcon.Id == y.UnresolvedExpandedIcon.Id &&
                       x.ImplicitIcon.Id == y.ImplicitIcon.Id &&
                       x.ImplicitExpandedIcon.Id == y.ImplicitExpandedIcon.Id &&
                       x.Icon.Guid == y.Icon.Guid &&
                       x.ExpandedIcon.Guid == y.ExpandedIcon.Guid &&
                       x.UnresolvedIcon.Guid == y.UnresolvedIcon.Guid &&
                       x.UnresolvedExpandedIcon.Guid == y.UnresolvedExpandedIcon.Guid &&
                       x.ImplicitIcon.Guid == y.ImplicitIcon.Guid &&
                       x.ImplicitExpandedIcon.Guid == y.ImplicitExpandedIcon.Guid;
            }

            public int GetHashCode((ImageMoniker Icon, ImageMoniker ExpandedIcon, ImageMoniker UnresolvedIcon, ImageMoniker UnresolvedExpandedIcon, ImageMoniker ImplicitIcon, ImageMoniker ImplicitExpandedIcon) obj)
            {
                int hashCode = obj.Icon.Id;
                hashCode = (hashCode * -1521134295) ^ obj.Icon.Guid.GetHashCode();
                hashCode = (hashCode * -1521134295) ^ obj.ExpandedIcon.Id;
                hashCode = (hashCode * -1521134295) ^ obj.ExpandedIcon.Guid.GetHashCode();
                hashCode = (hashCode * -1521134295) ^ obj.UnresolvedIcon.Id;
                hashCode = (hashCode * -1521134295) ^ obj.UnresolvedIcon.Guid.GetHashCode();
                hashCode = (hashCode * -1521134295) ^ obj.UnresolvedExpandedIcon.Id;
                hashCode = (hashCode * -1521134295) ^ obj.UnresolvedExpandedIcon.Guid.GetHashCode();
                hashCode = (hashCode * -1521134295) ^ obj.ImplicitIcon.Id;
                hashCode = (hashCode * -1521134295) ^ obj.ImplicitExpandedIcon.Guid.GetHashCode();
                return hashCode;
            }
        }
    }
}
