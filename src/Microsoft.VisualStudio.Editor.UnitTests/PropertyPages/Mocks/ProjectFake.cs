using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    [CLSCompliant(false)]
    public class ProjectFake : Project
    {
        public DTEFake Fake_dte;
        public ProjectFake(DTEFake dte)
        {
            Fake_dte = dte;
        }

        #region Project Members

        CodeModel Project.CodeModel
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Projects Project.Collection
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        ConfigurationManager Project.ConfigurationManager
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        DTE Project.DTE
        {
            get
            {
                return Fake_dte;
            }
        }

        void Project.Delete()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        string Project.ExtenderCATID
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object Project.ExtenderNames
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string Project.FileName
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string Project.FullName
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Globals Project.Globals
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        bool Project.IsDirty
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string Project.Kind
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string Project.Name
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        object Project.Object
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        ProjectItem Project.ParentProjectItem
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        ProjectItems Project.ProjectItems
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Properties Project.Properties
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        void Project.Save(string FileName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void Project.SaveAs(string NewFileName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        bool Project.Saved
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string Project.UniqueName
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object Project.get_Extender(string ExtenderName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
