using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    [CLSCompliant(false)]
    public class VsProjectSpecialFiles : IVsProjectSpecialFiles
    {
        public SpecialFile Fake_SettingsSpecialFile;
        public SpecialFile Fake_ResxSpecialFile;
        public SpecialFile Fake_AppXamlSpecialFile;

        public VsProjectSpecialFiles(VsHierarchyFake hierarchyFake)
        {
            Fake_SettingsSpecialFile = new SpecialFile(hierarchyFake, true, @"c:\temp\WindowsApplication\My Project\Settings.settings", true, "EmbeddedResource");
            Fake_ResxSpecialFile = new SpecialFile(hierarchyFake, true, @"c:\temp\WindowsApplication\My Project\Resources.resx", true, "EmbeddedResource");
            Fake_AppXamlSpecialFile = new SpecialFile(hierarchyFake, true, @"c:\temp\WindowsApplication\Application.xaml", true, "ApplicationDefinition");

        }

        public class SpecialFile
        {
            public bool supported;
            private string m_pathToCreate;
            private VsHierarchyFake m_hierarchyFake;
            private uint m_itemid;
            private string m_initialBuildAction;

            public SpecialFile(VsHierarchyFake hierarchyFake, bool supported, string pathToCreate, bool exists, string initialBuildAction)
            {
                m_initialBuildAction = initialBuildAction;
                m_hierarchyFake = hierarchyFake;
                this.supported = supported;
                m_pathToCreate = pathToCreate;
                if (supported && exists)
                    CreateFile();
            }

            public void CreateFile()
            {
                ProjectItemFake projectItemFake = this.ProjectItem;
                if (projectItemFake != null)
                    return;

                projectItemFake = new ProjectItemWithBuildActionFake(m_pathToCreate, m_pathToCreate, "None");
                projectItemFake.Fake_PropertiesCollection.Fake_PropertiesDictionary["ItemType"].Value = m_initialBuildAction;
                m_hierarchyFake.Fake_AddProjectItem(projectItemFake);
                m_itemid = projectItemFake.ItemId;
            }

            public void DeleteFile()
            {
                ProjectItemFake projectItemFake = ProjectItem;
                if (projectItemFake != null)
                {
                    m_hierarchyFake.Fake_projectItems.Remove(projectItemFake.ItemId);
                }
                m_itemid = 0xFFFFFFFF;
            }

            public ProjectItemFake ProjectItem
            {
                get
                {
                    if (m_hierarchyFake.Fake_projectItems.ContainsKey(m_itemid))
                        return (ProjectItemFake)m_hierarchyFake.Fake_projectItems[m_itemid];

                    return null;
                }
            }

            public int GetFile(int fileID, uint grfFlags, out uint pitemid, out string pbstrFilename)
            {
                pitemid = 0;
                pbstrFilename = null;

                if (!supported)
                {
                    return VSConstants.E_NOTIMPL;
                }

                ProjectItem projectItem = this.ProjectItem;

                if (projectItem == null && 0 != (grfFlags & (uint)__PSFFLAGS.PSFF_CreateIfNotExist))
                {
                    // Create it.
                    CreateFile();
                }

                pitemid = ((ProjectItemFake)projectItem).ItemId;
                pbstrFilename = projectItem.get_FileNames(1);
                return VSConstants.S_OK;
            }
        }

        #region IVsProjectSpecialFiles Members

        int IVsProjectSpecialFiles.GetFile(int fileID, uint grfFlags, out uint pitemid, out string pbstrFilename)
        {
            pitemid = 0;
            pbstrFilename = null;
            const int PSFFILEID_AppXaml = -1008;

            switch (fileID)
            {
                case (int)__PSFFILEID2.PSFFILEID_AppSettings:
                    return Fake_SettingsSpecialFile.GetFile(fileID, grfFlags, out pitemid, out pbstrFilename);

                case (int)__PSFFILEID2.PSFFILEID_AssemblyResource:
                    return Fake_ResxSpecialFile.GetFile(fileID, grfFlags, out pitemid, out pbstrFilename);

                case (int)PSFFILEID_AppXaml:
                    return Fake_AppXamlSpecialFile.GetFile(fileID, grfFlags, out pitemid, out pbstrFilename);

                default:
                    return VSConstants.E_NOTIMPL;
            }
        }

        #endregion
    }

}
