// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    /// <summary>
    /// Represents the set of icons associated with a particular dependency.
    /// In practice dependencies use a relatively small number of distinct icon
    /// sets; we can save considerable amounts of memory by giving the sets their
    /// own type and sharing instances.
    /// </summary>
    internal sealed class DependencyIconSet : IEquatable<DependencyIconSet?>
    {
        public DependencyIconSet(ImageMoniker icon, ImageMoniker expandedIcon, ImageMoniker unresolvedIcon, ImageMoniker unresolvedExpandedIcon, ImageMoniker implicitIcon, ImageMoniker implicitExpandedIcon)
        {
            Icon = icon;
            ExpandedIcon = expandedIcon;
            UnresolvedIcon = unresolvedIcon;
            UnresolvedExpandedIcon = unresolvedExpandedIcon;
            ImplicitIcon = implicitIcon;
            ImplicitExpandedIcon = implicitExpandedIcon;
        }

        /// <summary>
        /// Gets the icon to use when the dependency is resolved and collapsed.
        /// </summary>
        public ImageMoniker Icon { get; }

        /// <summary>
        /// Gets the icon to use when the dependency is resolved and expanded.
        /// </summary>
        public ImageMoniker ExpandedIcon { get; }

        /// <summary>
        /// Gets the icon to use when the dependency is unresolved and collapsed.
        /// </summary>
        public ImageMoniker UnresolvedIcon { get; }

        /// <summary>
        /// Gets the icon to use when the dependency is unresolved and expanded.
        /// </summary>
        public ImageMoniker UnresolvedExpandedIcon { get; }

        /// <summary>
        /// Gets the icon to use when the dependency is implicit (cannot be removed).
        /// </summary>
        public ImageMoniker ImplicitIcon { get; }

        /// <summary>
        /// Gets the icon to use when the dependency is implicit (cannot be removed) and expanded.
        /// </summary>
        public ImageMoniker ImplicitExpandedIcon { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as DependencyIconSet);
        }

        public bool Equals(DependencyIconSet? other)
        {
            return other is not null
                && Icon.Id == other.Icon.Id
                && ExpandedIcon.Id == other.ExpandedIcon.Id
                && UnresolvedIcon.Id == other.UnresolvedIcon.Id
                && UnresolvedExpandedIcon.Id == other.UnresolvedExpandedIcon.Id
                && ImplicitIcon.Id == other.ImplicitIcon.Id
                && ImplicitExpandedIcon.Id == other.ImplicitExpandedIcon.Id
                && Icon.Guid == other.Icon.Guid
                && ExpandedIcon.Guid == other.ExpandedIcon.Guid
                && UnresolvedIcon.Guid == other.UnresolvedIcon.Guid
                && UnresolvedExpandedIcon.Guid == other.UnresolvedExpandedIcon.Guid
                && ImplicitIcon.Guid == other.ImplicitIcon.Guid
                && ImplicitExpandedIcon.Guid == other.ImplicitExpandedIcon.Guid;
        }

        public override int GetHashCode()
        {
            int hashCode = Icon.Id.GetHashCode();
            hashCode = (hashCode * -1521134295) + Icon.Guid.GetHashCode();
            hashCode = (hashCode * -1521134295) + ExpandedIcon.Id.GetHashCode();
            hashCode = (hashCode * -1521134295) + ExpandedIcon.Guid.GetHashCode();
            hashCode = (hashCode * -1521134295) + UnresolvedIcon.Id.GetHashCode();
            hashCode = (hashCode * -1521134295) + UnresolvedIcon.Guid.GetHashCode();
            hashCode = (hashCode * -1521134295) + UnresolvedExpandedIcon.Id.GetHashCode();
            hashCode = (hashCode * -1521134295) + UnresolvedExpandedIcon.Guid.GetHashCode();
            hashCode = (hashCode * -1521134295) + ImplicitIcon.Id.GetHashCode();
            hashCode = (hashCode * -1521134295) + ImplicitExpandedIcon.Guid.GetHashCode();
            return hashCode;
        }
    }
}
