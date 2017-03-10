// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    [CLSCompliant(false)]
    public class VsHierarchyFake : IVsHierarchy, IVsProjectSpecialFiles, IVsProjectBuildSystem
    {
        public ProjectFake Fake_project;
        public DTEFake Fake_dte;
        public ProjectPropertiesFake Fake_projectProperties;
        public VsProjectSpecialFiles Fake_vsProjectSpecialFiles;
        public string Fake_supportedMyApplicationTypes = "WindowsApp;WindowsClassLib;CommandLineApp;WindowsService;WebControl";
        public Dictionary<uint, ProjectItem> Fake_projectItems = new Dictionary<uint, ProjectItem>();

        public VsHierarchyFake(DTEFake dte)
        {
            Fake_dte = dte;
            Fake_project = new ProjectFake(Fake_dte);
            Fake_projectProperties = new ProjectPropertiesFake(this);
            Fake_vsProjectSpecialFiles = new VsProjectSpecialFiles(this);
        }

        public void Fake_AddProjectItem(ProjectItemFake projectitem)
        {
            Fake_projectItems.Add(projectitem.ItemId, projectitem);
        }

        #region IVsHierarchy Members

        int IVsHierarchy.AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Close()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.GetCanonicalName(uint itemid, out string pbstrName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.GetGuidProperty(uint itemid, int propid, out Guid pguid)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.GetProperty(uint itemid, int propid, out object pvar)
        {
            switch ((__VSHPROPID)propid)
            {
                case __VSHPROPID.VSHPROPID_ExtObject:
                    if ((VSITEMID)itemid == VSITEMID.ROOT)
                    {
                        pvar = Fake_project;
                        return VSConstants.S_OK;
                    }

                    pvar = Fake_projectItems[itemid];
                    return VSConstants.S_OK;

                case __VSHPROPID.VSHPROPID_BrowseObject:
                    if ((VSITEMID)itemid == VSITEMID.ROOT)
                    {
                        pvar = Fake_projectProperties;
                        return VSConstants.S_OK;
                    }
                    throw new NotImplementedException();

                case (__VSHPROPID)__VSHPROPID2.VSHPROPID_SupportedMyApplicationTypes:
                    if ((VSITEMID)itemid == VSITEMID.ROOT)
                    {
                        pvar = Fake_supportedMyApplicationTypes;
                        return VSConstants.S_OK;
                    }
                    throw new ArgumentException();
            }

            throw new NotImplementedException("Property not yet implemented in fake");
        }

        int IVsHierarchy.GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.ParseCanonicalName(string pszName, out uint pitemid)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.QueryClose(out int pfCanClose)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.SetGuidProperty(uint itemid, int propid, ref Guid rguid)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.SetProperty(uint itemid, int propid, object var)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.UnadviseHierarchyEvents(uint dwCookie)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Unused0()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Unused1()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Unused2()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Unused3()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Unused4()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IVsProjectSpecialFiles Members

        int IVsProjectSpecialFiles.GetFile(int fileID, uint grfFlags, out uint pitemid, out string pbstrFilename)
        {
            return ((IVsProjectSpecialFiles)Fake_vsProjectSpecialFiles).GetFile(fileID, grfFlags, out pitemid, out pbstrFilename);
        }

        #endregion

        #region IVsProjectBuildSystem Members

        int IVsProjectBuildSystem.BuildTarget(string pszTargetName, out bool pbSuccess)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsProjectBuildSystem.CancelBatchEdit()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsProjectBuildSystem.EndBatchEdit()
        {
            //NYI
            return VSConstants.S_OK;
        }

        int IVsProjectBuildSystem.GetBuildSystemKind(out uint pBuildSystemKind)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsProjectBuildSystem.SetHostObject(string pszTargetName, string pszTaskName, object punkHostObject)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsProjectBuildSystem.StartBatchEdit()
        {
            //NYI
            return VSConstants.S_OK;
        }

        #endregion
    }

}
