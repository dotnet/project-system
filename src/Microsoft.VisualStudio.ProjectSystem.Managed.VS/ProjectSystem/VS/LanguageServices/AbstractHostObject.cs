// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IOLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{

    /// <summary>
    /// Base type for language service host object for cross targeting projects.
    /// </summary>
    internal abstract class AbstractHostObject : IVsHierarchy, IVsContainedLanguageProjectNameProvider, IVsProject
    {
        protected AbstractHostObject(IVsHierarchy innerHierarchy, IVsProject innerVsProject)
        {
            Requires.NotNull(innerHierarchy, nameof(innerHierarchy));
            Requires.NotNull(innerVsProject, nameof(innerVsProject));

            InnerHierarchy = innerHierarchy;
            InnerVsProject = innerVsProject;
        }

        protected IVsHierarchy InnerHierarchy { get; }
        protected IVsProject InnerVsProject { get; }
        public abstract string ActiveIntellisenseProjectDisplayName { get; }

        #region IVsHierarchy members

        public virtual int AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            return InnerHierarchy.AdviseHierarchyEvents(pEventSink, out pdwCookie);
        }

        public virtual int Close()
        {
            return InnerHierarchy.Close();
        }

        public virtual int GetCanonicalName(uint itemid, out string pbstrName)
        {
            return InnerHierarchy.GetCanonicalName(itemid, out pbstrName);
        }

        public virtual int GetGuidProperty(uint itemid, int propid, out Guid pguid)
        {
            return InnerHierarchy.GetGuidProperty(itemid, propid, out pguid);
        }

        public virtual int GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
        {
            return InnerHierarchy.GetNestedHierarchy(itemid, ref iidHierarchyNested, out ppHierarchyNested, out pitemidNested);
        }

        public virtual int GetProperty(uint itemid, int propid, out object pvar)
        {
            return InnerHierarchy.GetProperty(itemid, propid, out pvar);
        }

        public virtual int GetSite(out IOLEServiceProvider ppSP)
        {
            return InnerHierarchy.GetSite(out ppSP);
        }

        public virtual int ParseCanonicalName(string pszName, out uint pitemid)
        {
            return InnerHierarchy.ParseCanonicalName(pszName, out pitemid);
        }

        public virtual int QueryClose(out int pfCanClose)
        {
            return InnerHierarchy.QueryClose(out pfCanClose);
        }

        public int SetGuidProperty(uint itemid, int propid, ref Guid rguid)
        {
            return InnerHierarchy.SetGuidProperty(itemid, propid, ref rguid);
        }

        public virtual int SetProperty(uint itemid, int propid, object var)
        {
            return InnerHierarchy.SetProperty(itemid, propid, var);
        }

        public virtual int SetSite(IOLEServiceProvider psp)
        {
            return InnerHierarchy.SetSite(psp);
        }

        public virtual int UnadviseHierarchyEvents(uint dwCookie)
        {
            return InnerHierarchy.UnadviseHierarchyEvents(dwCookie);
        }

        public virtual int Unused0()
        {
            return InnerHierarchy.Unused0();
        }

        public virtual int Unused1()
        {
            return InnerHierarchy.Unused1();
        }

        public virtual int Unused2()
        {
            return InnerHierarchy.Unused2();
        }

        public virtual int Unused3()
        {
            return InnerHierarchy.Unused3();
        }

        public virtual int Unused4()
        {
            return InnerHierarchy.Unused4();
        }

        #endregion

        #region IVsContainedLanguageProjectNameProvider members

        public int GetProjectName(uint itemid, out string pbstrProjectName)
        {
            pbstrProjectName = ActiveIntellisenseProjectDisplayName;
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsProject members
        public int AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult)
        {
            return InnerVsProject.AddItem(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, pResult);
        }

        public int GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName)
        {
            return InnerVsProject.GenerateUniqueItemName(itemidLoc, pszExt, pszSuggestedRoot, out pbstrItemName);
        }

        public int GetItemContext(uint itemid, out IOLEServiceProvider ppSP)
        {
            return InnerVsProject.GetItemContext(itemid, out ppSP);
        }

        public int GetMkDocument(uint itemid, out string pbstrMkDocument)
        {
            return InnerVsProject.GetMkDocument(itemid, out pbstrMkDocument);
        }

        public int IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid)
        {
            return InnerVsProject.IsDocumentInProject(pszMkDocument, out pfFound, pdwPriority, out pitemid);
        }

        public int OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
        {
            return InnerVsProject.OpenItem(itemid, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
        }

        #endregion
    }
}
