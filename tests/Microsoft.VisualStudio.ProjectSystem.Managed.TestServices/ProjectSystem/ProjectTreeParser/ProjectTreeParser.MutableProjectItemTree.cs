// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal partial class ProjectTreeParser
    {
        private class MutableProjectItemTree : MutableProjectTree, IProjectItemTree2
        {
            public MutableProjectItemTree()
            {
                Item = new MutableProjectPropertiesContext();
            }

            public MutableProjectPropertiesContext Item { get; }

            public bool IsLinked => throw new NotImplementedException();

            public IPropertySheet PropertySheet => throw new NotImplementedException();

            public IProjectTree ClearItem()
            {
                throw new NotImplementedException();
            }

            public IProjectItemTree SetBrowseObjectProperties(IRule? browseObjectProperties)
            {
                throw new NotImplementedException();
            }

            public IProjectItemTree SetCaption(string caption)
            {
                throw new NotImplementedException();
            }

            public IProjectItemTree SetExpandedIcon(ProjectImageMoniker expandedIcon)
            {
                throw new NotImplementedException();
            }

            public IProjectItemTree SetIcon(ProjectImageMoniker icon)
            {
                throw new NotImplementedException();
            }

            public IProjectItemTree SetIsLinked(bool isLinked)
            {
                throw new NotImplementedException();
            }

            public IProjectItemTree SetItem(IProjectPropertiesContext projectPropertiesContext)
            {
                throw new NotImplementedException();
            }

            public IProjectItemTree SetPropertySheet(IPropertySheet propertySheet)
            {
                throw new NotImplementedException();
            }

            public IProjectItemTree SetVisible(bool visible)
            {
                throw new NotImplementedException();
            }

            IProjectItemTree IProjectItemTree.SetFlags(ProjectTreeFlags flags)
            {
                throw new NotImplementedException();
            }

            IProjectPropertiesContext IProjectItemTree.Item
            {
                get { return Item; }
            }

            IProjectItemTree2 IProjectItemTree2.SetProperties(string? caption, string? filePath, IRule? browseObjectProperties, ProjectImageMoniker? icon, ProjectImageMoniker? expandedIcon, bool? visible, ProjectTreeFlags? flags, IProjectPropertiesContext? context, IPropertySheet? propertySheet, bool? isLinked, bool resetFilePath, bool resetBrowseObjectProperties, bool resetIcon, bool resetExpandedIcon, int? displayOrder)
            {
                throw new NotImplementedException();
            }

            IProjectItemTree IProjectItemTree.SetProperties(string? caption, string? filePath, IRule? browseObjectProperties, ProjectImageMoniker? icon, ProjectImageMoniker? expandedIcon, bool? visible, ProjectTreeFlags? flags, IProjectPropertiesContext? context, IPropertySheet? propertySheet, bool? isLinked, bool resetFilePath, bool resetBrowseObjectProperties, bool resetIcon, bool resetExpandedIcon)
            {
                throw new NotImplementedException();
            }

            public MutableProjectTree BuildProjectTree()
            {
                // MutableProjectItemTree acts as a builder for both MutableProjectItemTree and MutableProjectTree
                // 
                // Once we've finished building, return either ourselves if we are already are a MutableProjectItemTree
                // otherwise, copy ourselves to a MutableProjectTree.

                if (!string.IsNullOrEmpty(Item.ItemName) || !string.IsNullOrEmpty(Item.ItemType))
                {
                    return this;
                }

                var tree = new MutableProjectTree();
                foreach (MutableProjectTree child in Children)
                {
                    tree.Children.Add(child);
                }

                tree.Caption = Caption;
                tree.Flags = Flags;
                tree.FilePath = FilePath;
                tree.Visible = Visible;
                tree.Parent = Parent;
                tree.Icon = Icon;
                tree.SubType = SubType;
                tree.ExpandedIcon = ExpandedIcon;
                tree.DisplayOrder = DisplayOrder;

                return tree;
            }
        }
    }
}
