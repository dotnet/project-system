// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    [CLSCompliant(false)]
    public class ProjectItemsFake : ProjectItems
    {
        public string Fake_name;
        public List<ProjectItemFake> Fake_listOfProjectItems = new List<ProjectItemFake>();

        public ProjectItemsFake(string name, params ProjectItemFake[] projectItems)
        {
            Fake_name = name;
            foreach (ProjectItemFake pi in projectItems)
            {
                Fake_listOfProjectItems.Add(pi);
            }
        }

        #region ProjectItems Members

        ProjectItem ProjectItems.AddFolder(string Name, string Kind)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        ProjectItem ProjectItems.AddFromDirectory(string Directory)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        ProjectItem ProjectItems.AddFromFile(string FileName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        ProjectItem ProjectItems.AddFromFileCopy(string FilePath)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        ProjectItem ProjectItems.AddFromTemplate(string FileName, string Name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        Project ProjectItems.ContainingProject
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        int ProjectItems.Count
        {
            get { return Fake_listOfProjectItems.Count; }
        }

        DTE ProjectItems.DTE
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        System.Collections.IEnumerator ProjectItems.GetEnumerator()
        {
            return Fake_listOfProjectItems.GetEnumerator();
        }

        ProjectItem ProjectItems.Item(object index)
        {
            if (index is int)
                return Fake_listOfProjectItems[(int)index];
            else
                throw new NotImplementedException();
        }

        string ProjectItems.Kind
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object ProjectItems.Parent
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Fake_listOfProjectItems.GetEnumerator();
        }

        #endregion
    }
}
