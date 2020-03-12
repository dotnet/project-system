// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        /// Finds a tree node by its flags. If there many nodes that satisfy flags, returns first.
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
