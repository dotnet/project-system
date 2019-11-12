// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectTreeExtensions
    {
        /// <summary>
        /// Gets the direct child of <paramref name="tree"/> with <paramref name="caption"/>
        /// if found, otherwise <see langword="null"/>.
        /// </summary>
        public static IProjectTree? FindChildWithCaption(this IProjectTree tree, string caption)
        {
            return tree.Children.FirstOrDefault(
                (child, cap) => string.Equals(cap, child.Caption, StringComparisons.ProjectTreeCaptionIgnoreCase),
                caption);
        }

        /// <summary>
        /// Finds a tree node by it's flags. If there many nodes that satisfy flags, returns first.
        /// </summary>
        internal static IProjectTree? GetSubTreeNode(this IProjectTree self, ProjectTreeFlags flags)
        {
            foreach (IProjectTree child in self.Children)
            {
                if (child.Flags.Contains(flags))
                {
                    return child;
                }
            }

            return null;
        }
    }
}
