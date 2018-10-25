// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectTreeExtensions
    {
        /// <summary>
        /// Gets the direct child of <paramref name="tree"/> with <paramref name="caption"/>
        /// if found, otherwise <see langword="null"/>.
        /// </summary>
        public static IProjectTree GetChildWithCaption(this IProjectTree tree, string caption)
        {
            return tree.Children.FirstOrDefault(
                child => string.Equals(caption, child.Caption, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns HierarchyId for given IProjectTree
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static HierarchyId GetHierarchyId(this IProjectTree tree) =>
            new HierarchyId(tree.IsRoot() ? VSConstants.VSITEMID_ROOT : unchecked((uint)tree.Identity));
    }
}
