// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS;
using EncInterop = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Managed.VS.EditAndContinue
{

    [ExportProjectNodeComService(typeof(IVsENCRebuildableProjectCfg), typeof(EncInterop.IVsENCRebuildableProjectCfg2), typeof(EncInterop.IVsENCRebuildableProjectCfg4))]
    [AppliesTo(ProjectCapability.EditAndContinue)]
    internal class EditAndContinueProvider : IVsENCRebuildableProjectCfg, EncInterop.IVsENCRebuildableProjectCfg2, EncInterop.IVsENCRebuildableProjectCfg4
    {
        private readonly ILanguageServiceHost _host;

        [ImportingConstructor]
        public EditAndContinueProvider(ILanguageServiceHost host)
        {
            _host = host;
        }

        // Different methods return different VSConstants(instead of VSConstants.E_FAIL) when HostSpecificEditAndContinueService is null.
        // The out parameters are assigned some default value in these methods. These are not random values.
        // These are the same values returned by the language service if its EnC component, to which language service delegate Enc Calls,
        // is null. We are retaining the same behavior here.
        // Similarly for implementation of IVsENCRebuildableProjectCfg4 as well
        public int BuildForEnc([In, MarshalAs(UnmanagedType.IUnknown)] object pUpdatePE)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.BuildForEnc(pUpdatePE) ?? VSConstants.S_OK;
        }

        public int EncApplySucceeded([In] int hrApplyResult)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.EncApplySucceeded(hrApplyResult) ?? VSConstants.S_OK;
        }

        public int EnterBreakStateOnPE([In] EncInterop.ENC_BREAKSTATE_REASON encBreakReason, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ENC_ACTIVE_STATEMENT[] pActiveStatements, [In] uint cActiveStatements)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.EnterBreakStateOnPE(encBreakReason, pActiveStatements, cActiveStatements) ?? VSConstants.S_OK;
        }

        public int ExitBreakStateOnPE()
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.ExitBreakStateOnPE() ?? VSConstants.S_OK;
        }

        public int GetCurrentActiveStatementPosition([In] uint id, [MarshalAs(UnmanagedType.LPArray), Out] TextSpan[] ptsNewPosition)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.GetCurrentActiveStatementPosition(id, ptsNewPosition) ?? VSConstants.E_FAIL;
        }

        public int GetCurrentExceptionSpanPosition([In] uint id, [MarshalAs(UnmanagedType.LPArray), Out] TextSpan[] ptsNewPosition)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.GetCurrentExceptionSpanPosition(id, ptsNewPosition) ?? VSConstants.E_FAIL;
        }

        public int GetENCBuildState([MarshalAs(UnmanagedType.LPArray), Out] ENC_BUILD_STATE[] pENCBuildState)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.GetENCBuildState(pENCBuildState) ?? VSConstants.E_FAIL;
        }

        public int GetExceptionSpanCount([Out] out uint pcExceptionSpan)
        {
            pcExceptionSpan = default(uint);
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.GetExceptionSpanCount(out pcExceptionSpan) ?? VSConstants.E_FAIL;
        }

        public int GetExceptionSpans([In] uint celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] ENC_EXCEPTION_SPAN[] rgelt, [In, Out] ref uint pceltFetched)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.GetExceptionSpans(celt, rgelt, ref pceltFetched) ?? VSConstants.E_FAIL;
        }

        public int GetPEBuildTimeStamp([MarshalAs(UnmanagedType.LPArray), Out] OLE.Interop.FILETIME[] pTimeStamp)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.GetPEBuildTimeStamp(pTimeStamp) ?? VSConstants.E_NOTIMPL;
        }

        public int GetPEidentity([MarshalAs(UnmanagedType.LPArray), Out] Guid[] pMVID, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr), Out] string[] pbstrPEName)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.GetPEidentity(pMVID, pbstrPEName) ?? VSConstants.E_FAIL;
        }

        public int StartDebuggingPE()
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.StartDebuggingPE() ?? VSConstants.S_OK;
        }

        public int StopDebuggingPE()
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.HostSpecificEditAndContinueService)?.StopDebuggingPE() ?? VSConstants.S_OK;
        }

        public int HasCustomMetadataEmitter([MarshalAs(UnmanagedType.VariantBool)] out bool value)
        {
            value = true;
            return ((EncInterop.IVsENCRebuildableProjectCfg4)_host.HostSpecificEditAndContinueService)?.HasCustomMetadataEmitter(out value) ?? VSConstants.S_OK;
        }

        // Managed ENC always uses IVsENCRebuildableProjectCfg2.
        // Although we don't implement the methods of IVsENCRebuildableProjectCfg, it is important that we implement the interface and return VSConstants.E_NOTIMPL.
        // This is how the EncManager recognizes the project system that supports EnC.
        public int ENCRebuild(object in_pProgram, out object out_ppSnapshot)
        {
            out_ppSnapshot = null;
            return VSConstants.E_NOTIMPL;
        }

        public int BelongToProject(string in_szFileName, ENC_REASON in_ENCReason, int in_fOnContinue)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int ENCComplete(int in_fENCSuccess)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int CancelENC()
        {
            return VSConstants.E_NOTIMPL;
        }

        public int ENCRelink([In, MarshalAs(UnmanagedType.IUnknown)] object pENCRelinkInfo)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int StartDebugging()
        {
            return VSConstants.E_NOTIMPL;
        }

        public int StopDebugging()
        {
            return VSConstants.E_NOTIMPL;
        }

        public int SetENCProjectBuildOption([In] ref Guid in_guidOption, [In, MarshalAs(UnmanagedType.LPWStr)] string in_szOptionValue)
        {
            return VSConstants.E_NOTIMPL;
        }

    }
}