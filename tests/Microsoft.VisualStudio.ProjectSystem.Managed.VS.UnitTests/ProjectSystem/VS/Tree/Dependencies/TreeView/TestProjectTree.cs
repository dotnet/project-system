// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Properties;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.TreeView
{
    internal class TestProjectTree : IProjectTree
    {
        public TestProjectTree()
        {
            Children = new ChildCollection(this);
        }

        public ICollection<TestProjectTree> Children { get; }

        public bool IsProjectItem { get; set; }

        // for scenario where we need to see if it was recreated or not
        public string CustomTag { get; set; }

        public int Size { get; }
        public IRule BrowseObjectProperties { get; set; }
        public ProjectTreeFlags Flags { get; set; } = ProjectTreeFlags.Empty;
        public bool IsFolder { get; }
        public bool Visible { get; set; }
        public ProjectImageMoniker ExpandedIcon { get; set; }
        public ProjectImageMoniker Icon { get; set; }
        public string FilePath { get; set; } = "";
        public string Caption { get; set; }
        IReadOnlyList<IProjectTree> IProjectTree.Children => Children.ToList();
        public IProjectTree Root { get; }
        public IProjectTree Parent { get; set; }
        public IntPtr Identity { get; }

        public IProjectTree Add(IProjectTree subtree)
        {
            ((TestProjectTree)subtree).Parent = this;
            Children.Add((TestProjectTree)subtree);
            return subtree;
        }

        public IProjectItemTree Add(IProjectItemTree subtree)
        {
            return null;
        }

        public IEnumerable<IProjectTreeDiff> ChangesSince(IProjectTree priorVersion)
        {
            return null;
        }

        public bool Contains(IntPtr nodeId)
        {
            return false;
        }

        public IProjectTree Find(IntPtr nodeId)
        {
            return null;
        }

        public IProjectTree Remove()
        {
            return Parent.Remove(this);
        }

        public IProjectTree Remove(IProjectTree subtree)
        {
            var nodeToRemove = Children.FirstOrDefault(x => x.FilePath.Equals(subtree.FilePath));
            if (nodeToRemove != null)
            {
                Children.Remove(nodeToRemove);
            }

            return this;
        }

        public IProjectItemTree Replace(IProjectItemTree subtree)
        {
            return null;
        }

        public IProjectTree Replace(IProjectTree subtree)
        {
            return null;
        }

        public IProjectTree SetBrowseObjectProperties(IRule browseObjectProperties)
        {
            return null;
        }

        public IProjectTree SetCaption(string caption)
        {
            return null;
        }

        public IProjectTree SetExpandedIcon(ProjectImageMoniker expandedIcon)
        {
            return null;
        }

        public IProjectTree SetFlags(ProjectTreeFlags flags)
        {
            return null;
        }

        public IProjectTree SetIcon(ProjectImageMoniker icon)
        {
            return null;
        }

        public IProjectItemTree SetItem(IProjectPropertiesContext context, IPropertySheet propertySheet, bool isLinked)
        {
            return null;
        }

        public IProjectTree SetProperties(string caption = null, string filePath = null, IRule browseObjectProperties = null,
                                          ProjectImageMoniker icon = null, ProjectImageMoniker expandedIcon = null, bool?
                                          visible = null, ProjectTreeFlags? flags = null, IProjectPropertiesContext context = null,
                                          IPropertySheet propertySheet = null, bool? isLinked = null, bool resetFilePath = false,
                                          bool resetBrowseObjectProperties = false, bool resetIcon = false, bool resetExpandedIcon = false)
        {
            Icon = icon ?? Icon;
            ExpandedIcon = expandedIcon ?? ExpandedIcon;
            BrowseObjectProperties = browseObjectProperties ?? BrowseObjectProperties;
            Caption = caption ?? Caption;

            return this;
        }

        public IProjectTree SetVisible(bool visible)
        {
            return null;
        }

        public bool TryFind(IntPtr nodeId, out IProjectTree subtree)
        {
            subtree = null;
            return false;
        }

        public bool TryFindImmediateChild(string caption, out IProjectTree subtree)
        {
            subtree = null;
            return false;
        }

        private sealed class ChildCollection : Collection<TestProjectTree>
        {
            private readonly TestProjectTree _parent;

            public ChildCollection(TestProjectTree parent)
            {
                _parent = parent;
            }

            protected override void InsertItem(int index, TestProjectTree item)
            {
                item.Parent = _parent;
                base.InsertItem(index, item);
            }

            protected override void SetItem(int index, TestProjectTree item)
            {
                item.Parent = _parent;
                base.SetItem(index, item);
            }
        }
    }
}
