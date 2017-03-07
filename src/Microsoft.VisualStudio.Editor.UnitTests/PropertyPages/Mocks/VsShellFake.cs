using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    class VsShellFake : IVsShell
    {
        #region IVsShell Members

        int IVsShell.AdviseBroadcastMessages(IVsBroadcastMessageEvents pSink, out uint pdwCookie)
        {
            //NYI
            pdwCookie = 1;
            return VSConstants.S_OK;
        }

        int IVsShell.AdviseShellPropertyChanges(IVsShellPropertyEvents pSink, out uint pdwCookie)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsShell.GetPackageEnum(out IEnumPackages ppenum)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsShell.GetProperty(int propid, out object pvar)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsShell.IsPackageInstalled(ref Guid guidPackage, out int pfInstalled)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsShell.IsPackageLoaded(ref Guid guidPackage, out IVsPackage ppPackage)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsShell.LoadPackage(ref Guid guidPackage, out IVsPackage ppPackage)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsShell.LoadPackageString(ref Guid guidPackage, uint resid, out string pbstrOut)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsShell.LoadUILibrary(ref Guid guidPackage, uint dwExFlags, out uint phinstOut)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsShell.SetProperty(int propid, object var)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsShell.UnadviseBroadcastMessages(uint dwCookie)
        {
            return VSConstants.S_OK;
        }

        int IVsShell.UnadviseShellPropertyChanges(uint dwCookie)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
