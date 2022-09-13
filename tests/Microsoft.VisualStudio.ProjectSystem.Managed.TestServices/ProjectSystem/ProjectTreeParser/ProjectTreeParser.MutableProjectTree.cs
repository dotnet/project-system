// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal partial class ProjectTreeParser
    {
        private partial class MutableProjectTree : IProjectTree2
        {
            public MutableProjectTree()
            {
                Children = new Collection<MutableProjectTree>();
                Visible = true;
                Caption = "";
                BrowseObjectProperties = new SubTypeRule(this);
            }

            public Collection<MutableProjectTree> Children { get; }

            public string Caption { get; set; }

            public ProjectTreeFlags Flags { get; set; }

            public string? SubType { get; set; }

            public bool IsFolder
            {
                get { return Flags.Contains(ProjectTreeFlags.Common.Folder); }
            }

            public string? FilePath { get; set; }

            public bool Visible { get; set; }

            public MutableProjectTree? Parent { get; set; }

            IProjectTree? IProjectTree.Parent
            {
                get { return Parent; }
            }

            public IRule BrowseObjectProperties { get; }

            IReadOnlyList<IProjectTree> IProjectTree.Children
            {
                get { return Children; }
            }

            public ProjectImageMoniker? Icon { get; set; }

            public ProjectImageMoniker? ExpandedIcon { get; set; }

            IntPtr IProjectTree.Identity
            {
                get
                {
                    return new IntPtr(Caption!.GetHashCode());
                }
            }

            IProjectTree IProjectTree.Root
            {
                get
                {
                    MutableProjectTree root = this;
                    while (root.Parent is not null)
                    {
                        root = root.Parent;
                    }

                    return root;
                }
            }

            public IProjectTree AddFlag(string flag)
            {
                if (!Flags.Contains(flag))
                    Flags = Flags.Add(flag);

                return this;
            }

            int IProjectTree.Size
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public int DisplayOrder { get; set; }

            IProjectItemTree IProjectTree.Add(IProjectItemTree subtree)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.Add(IProjectTree subtree)
            {
                if (subtree is MutableProjectTree mutableTree)
                {
                    Children.Add(mutableTree);
                }

                return this;
            }

            IEnumerable<IProjectTreeDiff> IProjectTree.ChangesSince(IProjectTree priorVersion)
            {
                throw new NotImplementedException();
            }

            bool IProjectTree.Contains(IntPtr nodeId)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.Find(IntPtr nodeId)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.Remove()
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.Remove(IProjectTree subtree)
            {
                if (subtree is MutableProjectTree mutableTree)
                {
                    if (Children.Contains(mutableTree))
                    {
                        Children.Remove(mutableTree);
                    }
                }

                return this;
            }

            IProjectItemTree IProjectTree.Replace(IProjectItemTree subtree)
            {
                return subtree;
            }

            IProjectTree IProjectTree.Replace(IProjectTree subtree)
            {
                return subtree;
            }

            IProjectTree IProjectTree.SetBrowseObjectProperties(IRule? browseObjectProperties)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetCaption(string caption)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetExpandedIcon(ProjectImageMoniker? expandedIcon)
            {
                ExpandedIcon = expandedIcon;

                return this;
            }

            IProjectTree IProjectTree.SetIcon(ProjectImageMoniker? icon)
            {
                Icon = icon;

                return this;
            }

            IProjectItemTree IProjectTree.SetItem(IProjectPropertiesContext context, IPropertySheet? propertySheet, bool isLinked)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetVisible(bool visible)
            {
                throw new NotImplementedException();
            }

            bool IProjectTree.TryFind(IntPtr nodeId, out IProjectTree subtree)
            {
                throw new NotImplementedException();
            }

            bool IProjectTree.TryFindImmediateChild(string caption, out IProjectTree subtree)
            {
                subtree = Children.FirstOrDefault(c => c.Caption == caption);
                return subtree is not null;
            }

            public IProjectTree SetProperties(string? caption = null, string? filePath = null, IRule? browseObjectProperties = null, ProjectImageMoniker? icon = null, ProjectImageMoniker? expandedIcon = null, bool? visible = null, ProjectTreeFlags? flags = null, IProjectPropertiesContext? context = null, IPropertySheet? propertySheet = null, bool? isLinked = null, bool resetFilePath = false, bool resetBrowseObjectProperties = false, bool resetIcon = false, bool resetExpandedIcon = false)
            {
                if (caption is not null)
                    Caption = caption;

                if (filePath is not null)
                    FilePath = filePath;

                if (visible != null)
                    Visible = visible.Value;

                if (flags is not null)
                    Flags = flags.Value;

                return this;
            }

            public IProjectTree SetFlags(ProjectTreeFlags flags)
            {
                Flags = flags;

                return this;
            }

            public IProjectTree2 SetProperties(string? caption = null, string? filePath = null, IRule? browseObjectProperties = null, ProjectImageMoniker? icon = null, ProjectImageMoniker? expandedIcon = null, bool? visible = null, ProjectTreeFlags? flags = null, IProjectPropertiesContext? context = null, IPropertySheet? propertySheet = null, bool? isLinked = null, bool resetFilePath = false, bool resetBrowseObjectProperties = false, bool resetIcon = false, bool resetExpandedIcon = false, int? displayOrder = null)
            {
                throw new NotImplementedException();
            }

            public IProjectTree2 SetDisplayOrder(int displayOrder)
            {
                DisplayOrder = displayOrder;

                return this;
            }
        }
    }
}
