// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.Testing
{
    partial class ProjectTreeParser
    {
        private class MutableProjectTree : IProjectTree
        {
            public MutableProjectTree()
            {
                Children = new Collection<MutableProjectTree>();
                Visible = true;
            }

            public Collection<MutableProjectTree> Children
            {
                get;
            }

            public string Caption
            {
                get;
                set;
            }

            public ProjectTreeFlags Flags
            {
                get;
                set;
            }

            public bool IsFolder
            {
                get { return Flags.Contains(ProjectTreeFlags.Common.Folder); }
            }

            public string FilePath
            {
                get;
                set;
            }

            public bool Visible
            {
                get;
                set;
            }

            public MutableProjectTree Parent
            {
                get;
                set;
            }

            IProjectTree IProjectTree.Parent
            {
                get { return Parent; }
            }

            IRule IProjectTree.BrowseObjectProperties
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            IReadOnlyList<IProjectTree> IProjectTree.Children
            {
                get { return Children; }
            }

            ProjectImageMoniker IProjectTree.ExpandedIcon
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ProjectImageMoniker Icon
            {
                get;
                set;
            }

            IntPtr IProjectTree.Identity
            {
                get
                {
                    throw new NotImplementedException();
                }
            }


            IProjectTree IProjectTree.Root
            {
                get
                {
                    var root = this;
                    while (root.Parent != null)
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

            IProjectItemTree IProjectTree.Add(IProjectItemTree subtree)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.Add(IProjectTree subtree)
            {
                throw new NotImplementedException();
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
                throw new NotImplementedException();
            }

            IProjectItemTree IProjectTree.Replace(IProjectItemTree subtree)
            {
                return subtree;
            }

            IProjectTree IProjectTree.Replace(IProjectTree subtree)
            {
                return subtree;
            }

            IProjectTree IProjectTree.SetBrowseObjectProperties(IRule browseObjectProperties)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetCaption(string caption)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetExpandedIcon(ProjectImageMoniker expandedIcon)
            {
                throw new NotImplementedException();
            }

            IProjectTree IProjectTree.SetIcon(ProjectImageMoniker icon)
            {
                Icon = icon;

                return this;
            }

            IProjectItemTree IProjectTree.SetItem(IProjectPropertiesContext context, IPropertySheet propertySheet, bool isLinked)
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
                throw new NotImplementedException();
            }

            public IProjectTree SetProperties(string caption = null, string filePath = null, IRule browseObjectProperties = null, ProjectImageMoniker icon = null, ProjectImageMoniker expandedIcon = null, bool? visible = default(bool?), ProjectTreeFlags? flags = default(ProjectTreeFlags?), IProjectPropertiesContext context = null, IPropertySheet propertySheet = null, bool? isLinked = default(bool?), bool resetFilePath = false, bool resetBrowseObjectProperties = false, bool resetIcon = false, bool resetExpandedIcon = false)
            {
                if (caption != null)
                    Caption = caption;

                if (FilePath != null)
                    FilePath = filePath;

                if (visible != null)
                    Visible = visible.Value;

                if (flags != null)
                    Flags = flags.Value;

                return this;
            }

            public IProjectTree SetFlags(ProjectTreeFlags flags)
            {
                Flags = flags;

                return this;
            }
        }
    }
}
