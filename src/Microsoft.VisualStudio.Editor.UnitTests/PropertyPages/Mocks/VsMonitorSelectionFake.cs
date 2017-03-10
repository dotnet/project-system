// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    class VsMonitorSelectionFake : IVsMonitorSelection
    {
        private const uint cookie_SolutionBuilding = 301;

        #region IVsMonitorSelection Members

        int IVsMonitorSelection.AdviseSelectionEvents(IVsSelectionEvents pSink, out uint pdwCookie)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsMonitorSelection.GetCmdUIContextCookie(ref Guid rguidCmdUI, out uint pdwCmdUICookie)
        {
            if (rguidCmdUI.Equals(new Guid(UIContextGuids.SolutionBuilding)))
            {
                pdwCmdUICookie = cookie_SolutionBuilding;
                return VSConstants.S_OK;
            }

            throw new NotImplementedException();
        }

        int IVsMonitorSelection.GetCurrentElementValue(uint elementid, out object pvarValue)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsMonitorSelection.GetCurrentSelection(out IntPtr ppHier, out uint pitemid, out IVsMultiItemSelect ppMIS, out IntPtr ppSC)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsMonitorSelection.IsCmdUIContextActive(uint dwCmdUICookie, out int pfActive)
        {
            switch(dwCmdUICookie)
            {
                case cookie_SolutionBuilding:
                    pfActive = 0; //NYI;
                    return VSConstants.S_OK;
            }

            throw new NotImplementedException();
        }

        int IVsMonitorSelection.SetCmdUIContext(uint dwCmdUICookie, int fActive)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsMonitorSelection.UnadviseSelectionEvents(uint dwCookie)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
