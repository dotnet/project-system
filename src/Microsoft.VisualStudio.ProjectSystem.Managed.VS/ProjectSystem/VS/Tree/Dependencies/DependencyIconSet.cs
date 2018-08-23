// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Represents the set of icons associated with a particular dependency.
    /// In practice dependencies use a relatively small number of distinct icon
    /// sets; we can save considerable amounts of memory by given the sets their
    /// own type and sharing instances.
    /// </summary>
    internal sealed class DependencyIconSet
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
    }
}
