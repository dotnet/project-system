// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ProjectTreePropertiesProviderExtensions
    {
        /// <summary>
        ///     Visits the entire tree, calling <see cref="IProjectTreePropertiesProvider.CalculatePropertyValues(IProjectTreeCustomizablePropertyContext, IProjectTreeCustomizablePropertyValues)"/>
        ///     for every node.
        /// </summary>
        public static IProjectTree ChangePropertyValuesForEntireTree(this IProjectTreePropertiesProvider propertiesProvider, IProjectTree tree)
        {
            tree = tree.Replace(ChangePropertyValues(propertiesProvider, tree));

            foreach (IProjectTree child in tree.Children)
            {
                tree = child.Replace(ChangePropertyValuesForEntireTree(propertiesProvider, child)).Parent;
            }
            
            return tree;
        }

        private static IProjectTree ChangePropertyValues(IProjectTreePropertiesProvider propertiesProvider, IProjectTree child)
        {
            var propertyContextAndValues = new ProjectTreeCustomizablePropertyContextAndValues(child);

            propertiesProvider.CalculatePropertyValues(propertyContextAndValues, propertyContextAndValues);

            return propertyContextAndValues.Tree;
        }

        private class ProjectTreeCustomizablePropertyContextAndValues : IProjectTreeCustomizablePropertyValues, IProjectTreeCustomizablePropertyContext
        {
            private IProjectTree _tree;

            public ProjectTreeCustomizablePropertyContextAndValues(IProjectTree tree)
            {
                _tree = tree;
            }

            public IProjectTree Tree
            {
                get { return _tree; }
            }

            public ProjectImageMoniker ExpandedIcon
            {
                get { return _tree.ExpandedIcon; }
                set { _tree = _tree.SetExpandedIcon(value); }
            }

            public ProjectTreeFlags Flags
            {
                get { return _tree.Flags; }
                set { _tree = _tree.SetFlags(value); }
            }

            public ProjectImageMoniker Icon
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
                    IProjectTree parent = _tree.Parent;
                    if (parent == null)
                        return ProjectTreeFlags.Empty;

                    return parent.Flags;
                }
            }

            public IImmutableDictionary<string, string> ProjectTreeSettings
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}
