// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ProjectTreePropertiesProviderExtensions
    {
        /// <summary>
        ///     Visits the entire tree, calling <see cref="IProjectTreePropertiesProvider.CalculatePropertyValues(IProjectTreeCustomizablePropertyContext, IProjectTreeCustomizablePropertyValues)"/>
        ///     for every node.
        /// </summary>
        public static IProjectTree ChangePropertyValuesForEntireTree(this IProjectTreePropertiesProvider propertiesProvider, IProjectTree tree, IImmutableDictionary<string, string>? projectTreeSettings = null)
        {
            // Cheat here, because the IProjectTree that we get from ProjectTreeParser is mutable, we want to clone it
            // so that any properties providers changes don't affect the "original" tree. If we implemented a completely
            // immutable tree, then we wouldn't have to do that - but that's currently a lot of work for test-only purposes.
            string treeAsString = ProjectTreeWriter.WriteToString(tree);

            return ChangePropertyValuesWalkingTree(propertiesProvider, ProjectTreeParser.Parse(treeAsString), projectTreeSettings);
        }

        private static IProjectTree ChangePropertyValuesWalkingTree(IProjectTreePropertiesProvider propertiesProvider, IProjectTree tree, IImmutableDictionary<string, string>? projectTreeSettings)
        {
            tree = ChangePropertyValues(propertiesProvider, tree, projectTreeSettings);

            foreach (IProjectTree child in tree.Children)
            {
                tree = ChangePropertyValuesWalkingTree(propertiesProvider, child, projectTreeSettings).Parent!;
            }

            return tree;
        }

        private static IProjectTree ChangePropertyValues(IProjectTreePropertiesProvider propertiesProvider, IProjectTree child, IImmutableDictionary<string, string>? projectTreeSettings)
        {
            var propertyContextAndValues = new ProjectTreeCustomizablePropertyContextAndValues(child, projectTreeSettings);

            propertiesProvider.CalculatePropertyValues(propertyContextAndValues, propertyContextAndValues);

            return propertyContextAndValues.Tree;
        }

        private class ProjectTreeCustomizablePropertyContextAndValues : IProjectTreeCustomizablePropertyValues, IProjectTreeCustomizablePropertyContext
        {
            private IProjectTree _tree;

            public ProjectTreeCustomizablePropertyContextAndValues(IProjectTree tree, IImmutableDictionary<string, string>? projectTreeSettings)
            {
                _tree = tree;
                ProjectTreeSettings = projectTreeSettings ?? ImmutableDictionary<string, string>.Empty;
            }

            public IProjectTree Tree
            {
                get { return _tree; }
            }

            public ProjectImageMoniker? ExpandedIcon
            {
                get { return _tree.ExpandedIcon; }
                set { _tree = _tree.SetExpandedIcon(value); }
            }

            public ProjectTreeFlags Flags
            {
                get { return _tree.Flags; }
                set { _tree = _tree.SetFlags(value); }
            }

            public ProjectImageMoniker? Icon
            {
                get { return _tree.Icon; }
                set { _tree = _tree.SetIcon(value); }
            }
            public bool ExistsOnDisk
            {
                get { return _tree.Flags.ContainsAny(ProjectTreeFlags.Common.FileOnDisk | ProjectTreeFlags.Common.Folder); }
            }

            public string ItemName
            {
                get { return _tree.Caption; }
            }

            public string ItemType
            {
                get { throw new NotImplementedException(); }
            }

            public IImmutableDictionary<string, string> Metadata
            {
                get { throw new NotImplementedException(); }
            }

            public ProjectTreeFlags ParentNodeFlags
            {
                get
                {
                    IProjectTree? parent = _tree.Parent;
                    if (parent is null)
                        return ProjectTreeFlags.Empty;

                    return parent.Flags;
                }
            }

            public IImmutableDictionary<string, string> ProjectTreeSettings { get; }

            public bool IsFolder => _tree.Flags.Contains(ProjectTreeFlags.Common.Folder);

            public bool IsNonFileSystemProjectItem => _tree.Flags.Contains(ProjectTreeFlags.Common.NonFileSystemProjectItem);
        }
    }
}
