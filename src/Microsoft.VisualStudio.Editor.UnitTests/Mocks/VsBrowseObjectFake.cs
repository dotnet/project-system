/*
 using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class VsBrowseObjectFake : IVsBrowseObject
    {
        public IVsHierarchy Fake_hierarchy;
        public uint Fake_itemId;

        public VsBrowseObjectFake(IVsHierarchy hierarchy, uint itemid)
        {
            Fake_hierarchy = hierarchy;
            Fake_itemId = itemid;
        }

        #region IVsBrowseObject Members

        int IVsBrowseObject.GetProjectItem(out IVsHierarchy pHier, out uint pItemid)
        {
            pHier = Fake_hierarchy;
            pItemid = Fake_itemId;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
*/
