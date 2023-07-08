// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem;

internal class TestProjectTree : IProjectTree2, IProjectItemTree2
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
    public string? FilePath { get; set; }
    public string Caption { get; set; } = "<no caption set>";
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

    public IProjectTree Remove() => Parent!.Remove(this);

    public IProjectTree Remove(IProjectTree subtree)
    {
        var nodeToRemove = Children.FirstOrDefault(ReferenceEquals, subtree);
        if (nodeToRemove is not null)
        {
            Children.Remove(nodeToRemove);
        }

        return this;
    }

    public IProjectTree SetProperties(
        string? caption = null,
        string? filePath = null,
        IRule? browseObjectProperties = null,
        ProjectImageMoniker? icon = null,
        ProjectImageMoniker? expandedIcon = null,
        bool? visible = null,
        ProjectTreeFlags? flags = null,
        IProjectPropertiesContext? context = null,
        IPropertySheet? propertySheet = null,
        bool? isLinked = null,
        bool resetFilePath = false,
        bool resetBrowseObjectProperties = false,
        bool resetIcon = false,
        bool resetExpandedIcon = false)
    {
        Icon = icon ?? Icon;
        ExpandedIcon = expandedIcon ?? ExpandedIcon;
        BrowseObjectProperties = browseObjectProperties ?? BrowseObjectProperties;
        Caption = caption ?? Caption;

        return this;
    }

    public IProjectTree2 SetProperties(
        string? caption = null,
        string? filePath = null,
        IRule? browseObjectProperties = null,
        ProjectImageMoniker? icon = null,
        ProjectImageMoniker? expandedIcon = null,
        bool? visible = null,
        ProjectTreeFlags? flags = null,
        IProjectPropertiesContext? context = null,
        IPropertySheet? propertySheet = null,
        bool? isLinked = null,
        bool resetFilePath = false,
        bool resetBrowseObjectProperties = false,
        bool resetIcon = false,
        bool resetExpandedIcon = false,
        int? displayOrder = null)
    {
        Icon = icon ?? Icon;
        ExpandedIcon = expandedIcon ?? ExpandedIcon;
        BrowseObjectProperties = browseObjectProperties ?? BrowseObjectProperties;
        Caption = caption ?? Caption;

        return this;
    }

    #region Stubs

    IProjectItemTree IProjectTree.Add(IProjectItemTree subtree) => throw new NotImplementedException();
    IEnumerable<IProjectTreeDiff> IProjectTree.ChangesSince(IProjectTree priorVersion) => throw new NotImplementedException();
    bool IProjectTree.Contains(IntPtr nodeId) => throw new NotImplementedException();
    IProjectTree IProjectTree.Find(IntPtr nodeId) => throw new NotImplementedException();
    IProjectItemTree IProjectTree.Replace(IProjectItemTree subtree) => throw new NotImplementedException();
    IProjectTree IProjectTree.Replace(IProjectTree subtree) => throw new NotImplementedException();
    IProjectTree IProjectTree.SetBrowseObjectProperties(IRule? browseObjectProperties) => throw new NotImplementedException();
    IProjectTree IProjectTree.SetCaption(string caption) => throw new NotImplementedException();
    IProjectTree IProjectTree.SetExpandedIcon(ProjectImageMoniker? expandedIcon) => throw new NotImplementedException();
    IProjectTree IProjectTree.SetFlags(ProjectTreeFlags flags) => throw new NotImplementedException();
    IProjectTree IProjectTree.SetIcon(ProjectImageMoniker? icon) => throw new NotImplementedException();
    IProjectItemTree IProjectTree.SetItem(IProjectPropertiesContext context, IPropertySheet? propertySheet, bool isLinked) => throw new NotImplementedException();
    IProjectTree IProjectTree.SetVisible(bool visible) => throw new NotImplementedException();
    bool IProjectTree.TryFind(IntPtr nodeId, out IProjectTree subtree) => throw new NotImplementedException();
    bool IProjectTree.TryFindImmediateChild(string caption, out IProjectTree subtree) => throw new NotImplementedException();

    int IProjectTree2.DisplayOrder => throw new NotImplementedException();
    IProjectTree2 IProjectTree2.SetDisplayOrder(int displayOrder) => throw new NotImplementedException();

    bool IProjectItemTree.IsLinked => throw new NotImplementedException();
    IProjectPropertiesContext? IProjectItemTree.Item => throw new NotImplementedException();
    IPropertySheet? IProjectItemTree.PropertySheet => throw new NotImplementedException();
    IProjectItemTree IProjectItemTree.SetProperties(string? caption, string? filePath, IRule? browseObjectProperties, ProjectImageMoniker? icon, ProjectImageMoniker? expandedIcon, bool? visible, ProjectTreeFlags? flags, IProjectPropertiesContext? context, IPropertySheet? propertySheet, bool? isLinked, bool resetFilePath, bool resetBrowseObjectProperties, bool resetIcon, bool resetExpandedIcon) => throw new NotImplementedException();
    IProjectItemTree IProjectItemTree.SetCaption(string caption) => throw new NotImplementedException();
    IProjectItemTree IProjectItemTree.SetIcon(ProjectImageMoniker icon) => throw new NotImplementedException();
    IProjectItemTree IProjectItemTree.SetExpandedIcon(ProjectImageMoniker expandedIcon) => throw new NotImplementedException();
    IProjectItemTree IProjectItemTree.SetVisible(bool visible) => throw new NotImplementedException();
    IProjectItemTree IProjectItemTree.SetBrowseObjectProperties(IRule? browseObjectProperties) => throw new NotImplementedException();
    IProjectItemTree IProjectItemTree.SetFlags(ProjectTreeFlags flags) => throw new NotImplementedException();
    IProjectItemTree IProjectItemTree.SetIsLinked(bool isLinked) => throw new NotImplementedException();
    IProjectItemTree IProjectItemTree.SetItem(IProjectPropertiesContext projectPropertiesContext) => throw new NotImplementedException();
    IProjectItemTree IProjectItemTree.SetPropertySheet(IPropertySheet propertySheet) => throw new NotImplementedException();
    IProjectTree IProjectItemTree.ClearItem() => throw new NotImplementedException();

    IProjectItemTree2 IProjectItemTree2.SetProperties(string? caption, string? filePath, IRule? browseObjectProperties, ProjectImageMoniker? icon, ProjectImageMoniker? expandedIcon, bool? visible, ProjectTreeFlags? flags, IProjectPropertiesContext? context, IPropertySheet? propertySheet, bool? isLinked, bool resetFilePath, bool resetBrowseObjectProperties, bool resetIcon, bool resetExpandedIcon, int? displayOrder) => throw new NotImplementedException();
    IProjectTree2 IProjectItemTree2.SetDisplayOrder(int displayOrder) => throw new NotImplementedException();

    #endregion

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
