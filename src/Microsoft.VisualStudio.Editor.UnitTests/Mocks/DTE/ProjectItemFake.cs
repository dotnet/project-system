// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    [CLSCompliant(false)]
    public class ProjectItemFake : ProjectItem
    {
        private string m_relativePath;
        private string m_absolutePath;
        private ProjectItemsFake Fake_projectItems = new ProjectItemsFake("dependent files fake");
        private uint m_itemid;
        private static uint s_lastItemId = 0;

        public ProjectItemFake(string basePath, string relativePath)
            : this(basePath, relativePath, null)
        {
        }

        public ProjectItemFake(string basePath, string relativePath, ProjectItemsFake projectItems)
        {
            s_lastItemId += 1;
            m_itemid = s_lastItemId;

            m_absolutePath = System.IO.Path.Combine(basePath, relativePath);
            m_relativePath = relativePath;
            if (projectItems != null)
            {
                Fake_projectItems = projectItems;
            }
        }

        public ProjectItemFake()
            : this("WindowsApplication1", "fake project item.vb")
        {
        }

        public uint ItemId
        {
            get
            {
                return m_itemid;
            }
        }

        #region ProjectItem Members

        public PropertiesFake Fake_PropertiesCollection = new PropertiesFake();

        Properties ProjectItem.Properties
        {
            get
            {
                return Fake_PropertiesCollection;
            }
        }

        ProjectItems ProjectItem.Collection
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        ConfigurationManager ProjectItem.ConfigurationManager
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        Project ProjectItem.ContainingProject
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        DTE ProjectItem.DTE
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        void ProjectItem.Delete()
        {
            throw new Exception("ProjectItemFake: The method or operation is not implemented.");
        }

        Document ProjectItem.Document
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        void ProjectItem.ExpandView()
        {
            throw new Exception("ProjectItemFake: The method or operation is not implemented.");
        }

        string ProjectItem.ExtenderCATID
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        object ProjectItem.ExtenderNames
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        FileCodeModel ProjectItem.FileCodeModel
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        short ProjectItem.FileCount
        {
            get
            {
                return 1;
            }
        }

        bool ProjectItem.IsDirty
        {
            get
            {
                throw new Exception("ProjectItemFake: The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("ProjectItemFake: The method or operation is not implemented.");
            }
        }

        string ProjectItem.Kind
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        string ProjectItem.Name
        {
            get
            {
                return m_relativePath;
            }
            set
            {
                throw new Exception("ProjectItemFake: The method or operation is not implemented.");
            }
        }

        object ProjectItem.Object
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        Window ProjectItem.Open(string ViewKind)
        {
            throw new Exception("ProjectItemFake: The method or operation is not implemented.");
        }

        ProjectItems ProjectItem.ProjectItems
        {
            get
            {
                return Fake_projectItems;
            }
        }

        void ProjectItem.Remove()
        {
            throw new Exception("ProjectItemFake: The method or operation is not implemented.");
        }

        void ProjectItem.Save(string FileName)
        {
            throw new Exception("ProjectItemFake: The method or operation is not implemented.");
        }

        bool ProjectItem.SaveAs(string NewFileName)
        {
            throw new Exception("ProjectItemFake: The method or operation is not implemented.");
        }

        bool ProjectItem.Saved
        {
            get
            {
                throw new Exception("ProjectItemFake: The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("ProjectItemFake: The method or operation is not implemented.");
            }
        }

        Project ProjectItem.SubProject
        {
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
        }

        object ProjectItem.get_Extender(string ExtenderName)
        {
            throw new Exception("ProjectItemFake: The method or operation is not implemented.");
        }

        string ProjectItem.get_FileNames(short index)
        {
            if (index == 1)
            {
                return m_absolutePath;
            }
            else
            {
                throw new ArgumentException("index");
            }

        }

        bool ProjectItem.get_IsOpen(string ViewKind)
        {
            throw new Exception("ProjectItemFake: The method or operation is not implemented.");
        }

        #endregion
    }

}
