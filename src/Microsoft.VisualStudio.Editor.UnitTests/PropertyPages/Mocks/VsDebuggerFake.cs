// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    class VsDebuggerFake : IVsDebugger
    {
        public DBGMODE Fake_currentDebugMode = DBGMODE.DBGMODE_Design;

        #region IVsDebugger Members

        int IVsDebugger.AdviseDebugEventCallback(object punkDebuggerEvents)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.AdviseDebuggerEvents(IVsDebuggerEvents pSink, out uint pdwCookie)
        {
            //NYI
            pdwCookie = 2;
            return VSConstants.S_OK;
        }

        int IVsDebugger.AllowEditsWhileDebugging(ref Guid guidLanguageService)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.ExecCmdForTextPos(VsTextPos[] pTextPos, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.GetDataTipValue(Microsoft.VisualStudio.TextManager.Interop.IVsTextLines pTextBuf, Microsoft.VisualStudio.TextManager.Interop.TextSpan[] pTS, string pszExpression, out string pbstrValue)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.GetENCUpdate(out object ppUpdate)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.GetMode(DBGMODE[] pdbgmode)
        {
            pdbgmode = new DBGMODE[] { Fake_currentDebugMode };
            return VSConstants.S_OK;
        }

        int IVsDebugger.InsertBreakpointByName(ref Guid guidLanguage, string pszCodeLocationText)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.IsBreakpointOnName(ref Guid guidLanguage, string pszCodeLocationText, out int pfIsBreakpoint)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.LaunchDebugTargets(uint cTargets, IntPtr rgDebugTargetInfo)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.ParseFileRedirection(string pszArgs, out string pbstrArgsProcessed, out IntPtr phStdInput, out IntPtr phStdOutput, out IntPtr phStdError)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.QueryStatusForTextPos(VsTextPos[] pTextPos, ref Guid pguidCmdGroup, uint cCmds, Microsoft.VisualStudio.OLE.Interop.OLECMD[] prgCmds, IntPtr pCmdText)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.RemoveBreakpointsByName(ref Guid guidLanguage, string pszCodeLocationText)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.ToggleBreakpointByName(ref Guid guidLanguage, string pszCodeLocationText)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.UnadviseDebugEventCallback(object punkDebuggerEvents)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsDebugger.UnadviseDebuggerEvents(uint dwCookie)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
