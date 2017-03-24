// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class VsCfgBrowseObjectFake : IVsCfgBrowseObject
    {

        #region IVsCfgBrowseObject Members

        int IVsCfgBrowseObject.GetCfg(out IVsCfg ppCfg)
        {
            throw new Exception("GetCfg: The method or operation is not implemented.");
        }

        int IVsCfgBrowseObject.GetProjectItem(out IVsHierarchy pHier, out uint pItemid)
        {
            throw new Exception("GetProjectItem: The method or operation is not implemented.");
        }

        #endregion

        #region IVsBrowseObject Members

        int IVsBrowseObject.GetProjectItem(out IVsHierarchy pHier, out uint pItemid)
        {
            throw new Exception("GetProjectItem: The method or operation is not implemented.");
        }

        #endregion
    }
}
