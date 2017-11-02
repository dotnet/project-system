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
        private const string FullPathProperty = "FullPath";
        private readonly char[] _separators = new char[]
        {
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar
        };
        private Dictionary<string, int> _displayOrderMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, int> _rootedOrderMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private UnconfiguredProject _project;
        private ImmutableHashSet<string> _allowedItemTypes;

        public TreeItemOrderPropertyProvider(IReadOnlyCollection<ProjectItemIdentity> orderedItems, UnconfiguredProject project)
        {
            _project = project;
            OrderedItems = orderedItems;

            _allowedItemTypes = OrderedItems.Select(p => p.ItemType).ToImmutableHashSet();

            ComputeIndices();
        }

        public IReadOnlyCollection<ProjectItemIdentity> OrderedItems { get; }

        /// <summary>
        /// Preorder folders and items that are provided as ordered evaluated includes
        /// </summary>
        private void ComputeIndices()
        {
            var duplicateFiles = OrderedItems
                .Select(p => Path.GetFileName(p.EvaluatedInclude))
                .GroupBy(file => file, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToImmutableHashSet();

            var index = 1;
            
            foreach (var item in OrderedItems)
            {
                var includeParts = item.EvaluatedInclude.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in includeParts)
                {
                    var rootedPath = duplicateFiles.Contains(part) ? _project.MakeRooted(item.EvaluatedInclude) : null;
                    if (rootedPath != null && !_rootedOrderMap.ContainsKey(rootedPath))
                    {
                        _rootedOrderMap.Add(rootedPath, index++);
                    }
                    else if (!_displayOrderMap.ContainsKey(part))
                    {
                        _displayOrderMap.Add(part, index++);
                    }
                }
            }
        }

        /// <summary>
        /// Assign a display order property to items that have previously been preordered
        /// or other (hidden) items under the project root that are not folders
        /// </summary>
        /// <param name="propertyContext">context for the tree item being evaluated</param>
        /// <param name="propertyValues">mutable properties that can be changed to affect display order etc</param>
        public void CalculatePropertyValues(
            IProjectTreeCustomizablePropertyContext propertyContext, 
            IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (propertyValues is IProjectTreeCustomizablePropertyValues2 propertyValues2)
            {
                var isAllowedItemType = propertyContext.ItemType != null 
                    && _allowedItemTypes.Contains(propertyContext.ItemType);

                // assign display order to folders and items that have a recognized 
                // item type and appear in order map
                bool hasDisplayOrder = false;
                if (isAllowedItemType || propertyContext.IsFolder)
                {
                    if (_displayOrderMap.TryGetValue(propertyContext.ItemName, out var index) 
                        || (propertyContext.Metadata.TryGetValue(FullPathProperty, out var fullPath)
                            && _rootedOrderMap.TryGetValue(fullPath, out index)))
                    {
                        propertyValues2.DisplayOrder = index;
                        hasDisplayOrder = true;
                    }
                }

                if (!hasDisplayOrder && propertyContext.ParentNodeFlags.Contains(ProjectTreeFlags.ProjectRoot) && !propertyContext.IsFolder)
                {
                    // move unordered non-folder items at project root to the end 
                    // (this will typically be hidden items visible on "Show All Files")
                    propertyValues2.DisplayOrder = _displayOrderMap.Count + 1;
                }
            }
        }
    }
}
