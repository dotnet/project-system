// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectTreeExtensions
    {
        /// <summary>
        /// Adds a givel list of elements to IProjectTree
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="itemsToAdd"></param>
        /// <param name="itemFilter"></param>
        /// <param name="nodeCreator"></param>
        /// <returns></returns>
        public static IProjectTree AddElementsToTree(this IProjectTree tree,
            IEnumerable<string> itemsToAdd,
            Func<string, bool> itemFilter,
            Func<string, IProjectTree> nodeCreator)
        {
            foreach (string item in itemsToAdd)
            {
                if (itemFilter(item))
                {
                    // Double check we don't already have this one -  we shouldn't
                    var existingNode = tree.FindNodeByPath(item);
                    System.Diagnostics.Debug.Assert(existingNode == null);
                    if (existingNode == null)
                    {
                        tree = tree.Add(nodeCreator(item)).Parent;
                    }
                }
            }
            return tree;
        }

        /// <summary>
        /// Removes given list of elements form IProjectTree
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="itemsToRemove"></param>
        /// <param name="itemFilter"></param>
        /// <returns></returns>
        public static IProjectTree RemoveElementsFromTree(this IProjectTree tree,
            IEnumerable<string> itemsToRemove,
            Func<string, bool> itemFilter)
        {
            foreach (string file in itemsToRemove)
            {
                if (itemFilter(file))
                {
                    // See if we already have this one -  we shouldn't
                    var existingNode = tree.FindNodeByPath(file);
                    System.Diagnostics.Debug.Assert(existingNode != null);
                    if (existingNode != null)
                    {
                        tree = existingNode.Remove();
                    }
                }
            }
            return tree;
        }

        /// <summary>
        /// Finds direct child of IProjectTree by it's path
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="itemPath"></param>
        /// <returns></returns>
        public static IProjectTree FindNodeByPath(this IProjectTree tree, string itemPath)
        {
            return FindNodeHelper(tree, itemPath, child => child.FilePath);
        }

        /// <summary>
        /// Finds direct child of IProjectTree by it's name
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public static IProjectTree FindNodeByName(this IProjectTree tree, string itemName)
        {
            return FindNodeHelper(tree, itemName, child => child.Caption);
        }

        private static IProjectTree FindNodeHelper(this IProjectTree tree, string itemToFind,
            Func<IProjectTree, string> childPropertyFunction)
        {
            Contract.Requires(tree != null);
            Contract.Requires(!string.IsNullOrEmpty(itemToFind));

            return tree.Children.OfType<IProjectTree>()
                .FirstOrDefault(child => string.Equals(itemToFind, childPropertyFunction(child), StringComparison.OrdinalIgnoreCase));
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
