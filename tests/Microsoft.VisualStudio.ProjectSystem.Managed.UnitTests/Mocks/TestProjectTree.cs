// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
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
        public string? CustomTag { get; set; }

        public int Size { get; }
        public IRule? BrowseObjectProperties { get; set; }
        public ProjectTreeFlags Flags { get; set; } = ProjectTreeFlags.Empty;
        public bool IsFolder { get; }
        public bool Visible { get; set; }
        public ProjectImageMoniker? ExpandedIcon { get; set; }
        public ProjectImageMoniker? Icon { get; set; }
        public string? FilePath => null;
        public string Caption { get; set; } = "Caption";
        IReadOnlyList<IProjectTree> IProjectTree.Children => Children.ToList();
        public IProjectTree Root { get; } = null!;
        public IProjectTree? Parent { get; set; }
        public IntPtr Identity { get; }

        public IProjectTree Add(IProjectTree subtree)
        {
            ((TestProjectTree)subtree).Parent = this;
            Children.Add((TestProjectTree)subtree);
            return subtree;
        }

        public IProjectItemTree Add(IProjectItemTree subtree)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IProjectTreeDiff> ChangesSince(IProjectTree priorVersion)
        {
            throw new NotImplementedException();
        }

        public bool Contains(IntPtr nodeId)
        {
            throw new NotImplementedException();
        }

        public IProjectTree Find(IntPtr nodeId)
        {
            throw new NotImplementedException();
        }

        public IProjectTree Remove()
        {
            Assumes.NotNull(Parent);
            return Parent.Remove(this);
        }

        public IProjectTree Remove(IProjectTree subtree)
        {
            var nodeToRemove = Children.FirstOrDefault(ReferenceEquals, subtree);
            if (nodeToRemove is not null)
            {
                Children.Remove(nodeToRemove);
            }

            return this;
        }

        public IProjectItemTree Replace(IProjectItemTree subtree)
        {
            throw new NotImplementedException();
        }

        public IProjectTree Replace(IProjectTree subtree)
        {
            throw new NotImplementedException();
        }

        public IProjectTree SetBrowseObjectProperties(IRule? browseObjectProperties)
        {
            throw new NotImplementedException();
        }

        public IProjectTree SetCaption(string caption)
        {
            throw new NotImplementedException();
        }

        public IProjectTree SetExpandedIcon(ProjectImageMoniker? expandedIcon)
        {
            throw new NotImplementedException();
        }

        public IProjectTree SetFlags(ProjectTreeFlags flags)
        {
            throw new NotImplementedException();
        }

        public IProjectTree SetIcon(ProjectImageMoniker? icon)
        {
            throw new NotImplementedException();
        }

        public IProjectItemTree SetItem(IProjectPropertiesContext context, IPropertySheet? propertySheet, bool isLinked)
        {
            throw new NotImplementedException();
        }

        public IProjectTree SetProperties(string? caption = null, string? filePath = null, IRule? browseObjectProperties = null,
                                          ProjectImageMoniker? icon = null, ProjectImageMoniker? expandedIcon = null, bool? visible = null,
                                          ProjectTreeFlags? flags = null, IProjectPropertiesContext? context = null,
                                          IPropertySheet? propertySheet = null, bool? isLinked = null, bool resetFilePath = false,
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
            throw new NotImplementedException();
        }

        public bool TryFind(IntPtr nodeId, out IProjectTree subtree)
        {
            throw new NotImplementedException();
        }

        public bool TryFindImmediateChild(string caption, out IProjectTree subtree)
        {
            throw new NotImplementedException();
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
