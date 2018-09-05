// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Represents the set of icons associated with a particular dependency.
    /// In practice dependencies use a relatively small number of distinct icon
    /// sets; we can save considerable amounts of memory by given the sets their
    /// own type and sharing instances.
    /// </summary>
    internal sealed class DependencyIconSet : IEquatable<DependencyIconSet>
    {
        public DependencyIconSet(ImageMoniker icon, ImageMoniker expandedIcon, ImageMoniker unresolvedIcon, ImageMoniker unresolvedExpandedIcon)
        {
            Icon = icon;
            ExpandedIcon = expandedIcon;
            UnresolvedIcon = unresolvedIcon;
            UnresolvedExpandedIcon = unresolvedExpandedIcon;
        }

        public ImageMoniker Icon { get; }
        public ImageMoniker ExpandedIcon { get; }
        public ImageMoniker UnresolvedIcon { get; }
        public ImageMoniker UnresolvedExpandedIcon { get; }

        public DependencyIconSet WithIcon(ImageMoniker newIcon)
        {
            if (Icon.Id == newIcon.Id && Icon.Guid == newIcon.Guid)
            {
                return this;
            }

            return new DependencyIconSet(newIcon, ExpandedIcon, UnresolvedIcon, UnresolvedExpandedIcon);
        }

        public DependencyIconSet WithExpandedIcon(ImageMoniker newExpandedIcon)
        {
            if (ExpandedIcon.Id == newExpandedIcon.Id && ExpandedIcon.Guid == newExpandedIcon.Guid)
            {
                return this;
            }

            return new DependencyIconSet(Icon, newExpandedIcon, UnresolvedIcon, UnresolvedExpandedIcon);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DependencyIconSet);
        }

        public bool Equals(DependencyIconSet other)
        {
            return other != null
                && Icon.Id == other.Icon.Id
                && ExpandedIcon.Id == other.ExpandedIcon.Id
                && UnresolvedIcon.Id == other.UnresolvedIcon.Id
                && UnresolvedExpandedIcon.Id == other.UnresolvedExpandedIcon.Id
                && Icon.Guid == other.Icon.Guid
                && ExpandedIcon.Guid == other.ExpandedIcon.Guid
                && UnresolvedIcon.Guid == other.UnresolvedIcon.Guid
                && UnresolvedExpandedIcon.Guid == other.UnresolvedExpandedIcon.Guid;
        }

        public override int GetHashCode()
        {
            var hashCode = Icon.Id.GetHashCode();
            hashCode = hashCode * -1521134295 + Icon.Guid.GetHashCode();
            hashCode = hashCode * -1521134295 + ExpandedIcon.Id.GetHashCode();
            hashCode = hashCode * -1521134295 + ExpandedIcon.Guid.GetHashCode();
            hashCode = hashCode * -1521134295 + UnresolvedIcon.Id.GetHashCode();
            hashCode = hashCode * -1521134295 + UnresolvedIcon.Guid.GetHashCode();
            hashCode = hashCode * -1521134295 + UnresolvedExpandedIcon.Id.GetHashCode();
            hashCode = hashCode * -1521134295 + UnresolvedExpandedIcon.Guid.GetHashCode();
            return hashCode;
        }
    }
}
