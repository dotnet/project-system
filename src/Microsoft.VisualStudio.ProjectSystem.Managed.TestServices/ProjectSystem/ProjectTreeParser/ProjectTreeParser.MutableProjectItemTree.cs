// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    partial class ProjectTreeParser
    {
        private class MutableProjectItemTree : MutableProjectTree, IProjectItemTree2
        {
            public bool IsLinked => throw new NotImplementedException();

            public IProjectPropertiesContext Item { get; set; }

            public IPropertySheet PropertySheet => throw new NotImplementedException();

            public IProjectTree ClearItem()
            {
                throw new NotImplementedException();
            }

            public IProjectItemTree SetBrowseObjectProperties(IRule browseObjectProperties)
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

            IProjectItemTree2 IProjectItemTree2.SetProperties(string caption, string filePath, IRule browseObjectProperties, ProjectImageMoniker icon, ProjectImageMoniker expandedIcon, bool? visible, ProjectTreeFlags? flags, IProjectPropertiesContext context, IPropertySheet propertySheet, bool? isLinked, bool resetFilePath, bool resetBrowseObjectProperties, bool resetIcon, bool resetExpandedIcon, int? displayOrder)
            {
                throw new NotImplementedException();
            }

            IProjectItemTree IProjectItemTree.SetProperties(string caption, string filePath, IRule browseObjectProperties, ProjectImageMoniker icon, ProjectImageMoniker expandedIcon, bool? visible, ProjectTreeFlags? flags, IProjectPropertiesContext context, IPropertySheet propertySheet, bool? isLinked, bool resetFilePath, bool resetBrowseObjectProperties, bool resetIcon, bool resetExpandedIcon)
            {
                throw new NotImplementedException();
            }
        }
    }
}
