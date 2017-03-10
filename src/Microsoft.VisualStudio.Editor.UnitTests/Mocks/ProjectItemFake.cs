// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class ProjectItemFake : ProjectItem
    {
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
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
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
                throw new Exception("ProjectItemFake: The method or operation is not implemented.");
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
            get { throw new Exception("ProjectItemFake: The method or operation is not implemented."); }
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
            throw new Exception("ProjectItemFake: The method or operation is not implemented.");
        }

        bool ProjectItem.get_IsOpen(string ViewKind)
        {
            throw new Exception("ProjectItemFake: The method or operation is not implemented.");
        }

        #endregion
    }

}
