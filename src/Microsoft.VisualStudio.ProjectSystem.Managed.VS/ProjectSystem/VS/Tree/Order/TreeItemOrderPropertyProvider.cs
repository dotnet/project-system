// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Order
{
    /// <summary>
    /// Provider that computes display order of tree items based on input ordering of
    /// evaluated includes from the project file.
    /// </summary>
    internal class TreeItemOrderPropertyProvider : IProjectTreePropertiesProvider
    {
        private readonly char[] _separators = new char[]
        {
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar
        };

        public IReadOnlyCollection<ProjectItemIdentity> OrderedItems { get; }

        public ImmutableHashSet<string> AllowedItemTypes { get; }

        public Dictionary<string, int> DisplayOrderMap { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public TreeItemOrderPropertyProvider(IReadOnlyCollection<ProjectItemIdentity> orderedItems)
        {
            OrderedItems = orderedItems;

            AllowedItemTypes = OrderedItems.Select(p => p.ItemType).ToImmutableHashSet();

            ComputeIndices(orderedItems);
        }

        /// <summary>
        /// Preorder folders and items that are provided as ordered evaluated includes
        /// </summary>
        /// <param name="orderedItems">ordered evaluated includes</param>
        private void ComputeIndices(IReadOnlyCollection<ProjectItemIdentity> orderedItems)
        {
            var index = 1;
            var parts = OrderedItems.SelectMany(p => p.EvaluatedInclude.Split(_separators));

            foreach (var item in parts)
            {
                if (!DisplayOrderMap.ContainsKey(item))
                {
                    DisplayOrderMap.Add(item, index++);
                }
            }
        }

        /// <summary>
        /// Assign a display order property to items that have previously been preordered
        /// or other (hidden) items under the project root that are not folders
        /// </summary>
        /// <param name="propertyContext"></param>
        /// <param name="propertyValues"></param>
        public void CalculatePropertyValues(
            IProjectTreeCustomizablePropertyContext propertyContext, 
            IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (propertyValues is IProjectTreeCustomizablePropertyValues2 propertyValues2)
            {
                var isAllowedItemType = propertyContext.ItemType != null 
                    && AllowedItemTypes.Contains(propertyContext.ItemType);

                // assign disply order to folders and items that have a recognized 
                // item type and appear in order map
                if ((isAllowedItemType || propertyContext.IsFolder) 
                    && DisplayOrderMap.TryGetValue(propertyContext.ItemName, out var index))
                {
                    propertyValues2.DisplayOrder = index;
                }
                else if (propertyContext.ParentNodeFlags.Contains(ProjectTreeFlags.ProjectRoot) && !propertyContext.IsFolder)
                {
                    // move unordered non-folder items at project root to the end 
                    // (this will typically be hidden items visible on "Show All Files")
                    propertyValues2.DisplayOrder = DisplayOrderMap.Count + 1;
                }
            }
        }
    }
}
