// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.Contracts;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectTreeExtensions
    {
        /// <summary>
        /// Gets the direct child of <paramref name="tree"/> with <paramref name="caption"/>
        /// if found, otherwise <see langword="null"/>.
        /// </summary>
        [Pure]
        public static IProjectTree? FindChildWithCaption(this IProjectTree tree, string caption)
        {
            return tree.Children.FirstOrDefault(
                (child, cap) => string.Equals(cap, child.Caption, StringComparisons.ProjectTreeCaptionIgnoreCase),
                caption);
        }

        /// <summary>
        /// Finds the first child node having <paramref name="flags"/>, or <see langword="null"/> if no child matches.
        /// </summary>
        [Pure]
        internal static IProjectTree? FindChildWithFlags(this IProjectTree self, ProjectTreeFlags flags)
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
