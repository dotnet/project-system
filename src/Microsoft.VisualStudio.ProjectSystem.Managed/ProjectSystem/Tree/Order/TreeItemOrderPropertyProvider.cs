// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Order;

/// <summary>
/// Provider that computes display order of tree items based on input ordering of
/// evaluated includes from the project file.
/// </summary>
internal class TreeItemOrderPropertyProvider : IProjectTreePropertiesProvider
{
    private const string FullPathProperty = "FullPath";

    private readonly Dictionary<string, int> _orderedMap;

    public TreeItemOrderPropertyProvider(IReadOnlyCollection<ProjectItemIdentity> orderedItems, UnconfiguredProject project)
    {
        _orderedMap = CreateOrderedMap(project, orderedItems);
    }

    private static string[] GetPathComponents(string evaluatedInclude)
    {
        return evaluatedInclude.Split(Delimiter.Path, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string[] GetPathFolders(string path)
    {
        return GetPathComponents(Path.GetDirectoryName(path));
    }

    /// <summary>
    /// Get the display path for an item. The display path is what you see visually in solution explorer.
    /// </summary>
    private static string GetDisplayPath(UnconfiguredProject project, ProjectItemIdentity item)
    {
        string? linkPath = item.LinkPath;

        if (!Strings.IsNullOrWhiteSpace(linkPath))
        {
            // This is a linked file.
            // We use the link path because that is the rendering/display path in solution explorer.
            return project.MakeRelative(linkPath);
        }

        return project.MakeRelative(item.EvaluatedInclude);
    }

    /// <summary>
    /// Create an ordered map.
    /// </summary>
    private static Dictionary<string, int> CreateOrderedMap(UnconfiguredProject project, IReadOnlyCollection<ProjectItemIdentity> orderedItems)
    {
        int displayOrder = 1;
        var orderedMap = new Dictionary<string, int>(StringComparers.ItemNames);

        foreach (ProjectItemIdentity item in orderedItems)
        {
            string displayPath = GetDisplayPath(project, item);
            string[] folders = GetPathFolders(displayPath);

            // We need assign the display order to folders first before the file.
            // These folders could be physical or virtual. Virtual coming from link paths.
            foreach (string folder in folders)
            {
                // Folders are special. 
                // FIXME: Due to the lack of metadata/info from property context 
                //     in CalculatePropertyValues, we use the folder's name to identify it.
                //
                // The problem with this approach is this scenario:
                //     Foo/Bar/File.fs
                //     Test/Bar/File.fs
                // In this case, any folder named "Bar" will always have the same display order as other "Bar" folders.
                //     Again, this is due to not having enough info in property context.
                if (!orderedMap.ContainsKey(folder))
                {
                    orderedMap.Add(folder, displayOrder++);
                }
            }

            string fullPath = project.MakeRooted(item.EvaluatedInclude);

            // We uniquely identify a file by its fullpath.
            if (!orderedMap.ContainsKey(fullPath))
            {
                orderedMap.Add(fullPath, displayOrder++);
            }
        }

        return orderedMap;
    }

    /// <summary>
    /// Tries to get a display order for a property context.
    /// </summary>
    private bool TryGetDisplayOrder(IProjectTreeCustomizablePropertyContext propertyContext, out int displayOrder)
    {
        displayOrder = 0;

        if (propertyContext.IsFolder)
        {
            // Due to the lack of metadata/info in property context, we can only look up
            //     a physical/virtual folder by its name alone.
            return _orderedMap.TryGetValue(propertyContext.ItemName, out displayOrder);
        }

        return propertyContext.Metadata is not null &&
            propertyContext.Metadata.TryGetValue(FullPathProperty, out string fullPath) &&
            _orderedMap.TryGetValue(fullPath, out displayOrder);
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
            // assign display order to folders and items that appear in order map
            if (TryGetDisplayOrder(propertyContext, out int displayOrder))
            {
                // sometimes these items temporarily have null item type. Ignore these cases
                if (propertyContext.ItemType is not null)
                {
                    propertyValues2.DisplayOrder = displayOrder;
                }
            }
            else if (!propertyContext.IsFolder)
            {
                // move unordered non-folder items to the end 
                // (this will typically be hidden items visible on "Show All Files")
                propertyValues2.DisplayOrder = int.MaxValue;
            }
        }
    }
}
