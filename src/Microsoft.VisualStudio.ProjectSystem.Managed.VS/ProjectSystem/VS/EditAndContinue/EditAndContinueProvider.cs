using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using EncInterop = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Managed.VS.EditAndContinue
{

    [Export(ExportContractNames.VsTypes.ProjectNodeComExtension)]
    [AppliesTo(ProjectCapability.ManagedEditAndContinue)]
    [ComServiceIid(typeof(IVsENCRebuildableProjectCfg))]
    [ComServiceIid(typeof(EncInterop.IVsENCRebuildableProjectCfg2))]
    [ComServiceIid(typeof(EncInterop.IVsENCRebuildableProjectCfg4))]
    internal class EditAndContinueProvider : IVsENCRebuildableProjectCfg, EncInterop.IVsENCRebuildableProjectCfg2, EncInterop.IVsENCRebuildableProjectCfg4
    {
        private readonly ILanguageServiceHost _host;

        [ImportingConstructor]
        public EditAndContinueProvider(ILanguageServiceHost host)
        {
            _host = host;
        }

        #region IVsENCRebuildableProjectCfg Implementation
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
        #endregion

        #region IVsENCRebuildableProjectCfg2 Implementation
        public int BuildForEnc([In, MarshalAs(UnmanagedType.IUnknown)] object pUpdatePE)
        {
            return _host.ENCProjectConfig2?.BuildForEnc(pUpdatePE) ?? VSConstants.S_OK;
        }

        public int EncApplySucceeded([In] int hrApplyResult)
        {
            return _host.ENCProjectConfig2?.EncApplySucceeded(hrApplyResult) ?? VSConstants.S_OK;
        }

        public int EnterBreakStateOnPE([In] EncInterop.ENC_BREAKSTATE_REASON encBreakReason, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ENC_ACTIVE_STATEMENT[] pActiveStatements, [In] uint cActiveStatements)
        {
            return _host.ENCProjectConfig2?.EnterBreakStateOnPE(encBreakReason, pActiveStatements, cActiveStatements) ?? VSConstants.S_OK;
        }

        public int ExitBreakStateOnPE()
        {
            return _host.ENCProjectConfig2?.ExitBreakStateOnPE() ?? VSConstants.S_OK;
        }

        public int GetCurrentActiveStatementPosition([In] uint id, [MarshalAs(UnmanagedType.LPArray), Out] TextSpan[] ptsNewPosition)
        {
            return _host.ENCProjectConfig2?.GetCurrentActiveStatementPosition(id, ptsNewPosition) ?? VSConstants.E_FAIL;
        }

        public int GetCurrentExceptionSpanPosition([In] uint id, [MarshalAs(UnmanagedType.LPArray), Out] TextSpan[] ptsNewPosition)
        {
            return _host.ENCProjectConfig2?.GetCurrentExceptionSpanPosition(id, ptsNewPosition) ?? VSConstants.E_FAIL;
        }

        public int GetENCBuildState([MarshalAs(UnmanagedType.LPArray), Out] ENC_BUILD_STATE[] pENCBuildState)
        {
            return _host.ENCProjectConfig2?.GetENCBuildState(pENCBuildState) ?? VSConstants.E_FAIL;
        }

        public int GetExceptionSpanCount([Out] out uint pcExceptionSpan)
        {
            pcExceptionSpan = default(uint);
            return _host.ENCProjectConfig2?.GetExceptionSpanCount(out pcExceptionSpan) ?? VSConstants.E_FAIL;
        }

        public int GetExceptionSpans([In] uint celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] ENC_EXCEPTION_SPAN[] rgelt, [In, Out] ref uint pceltFetched)
        {
            return _host.ENCProjectConfig2?.GetExceptionSpans(celt, rgelt, ref pceltFetched) ?? VSConstants.E_FAIL;
        }

        public int GetPEBuildTimeStamp([MarshalAs(UnmanagedType.LPArray), Out] OLE.Interop.FILETIME[] pTimeStamp)
        {
            return _host.ENCProjectConfig2?.GetPEBuildTimeStamp(pTimeStamp) ?? VSConstants.E_NOTIMPL;
        }

        public int GetPEidentity([MarshalAs(UnmanagedType.LPArray), Out] Guid[] pMVID, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr), Out] string[] pbstrPEName)
        {
            return _host.ENCProjectConfig2?.GetPEidentity(pMVID, pbstrPEName) ?? VSConstants.E_FAIL;
        }

        public int StartDebuggingPE()
        {
            return _host.ENCProjectConfig2?.StartDebuggingPE() ?? VSConstants.S_OK;
        }

        public int StopDebuggingPE()
        {
            return _host.ENCProjectConfig2?.StopDebuggingPE() ?? VSConstants.S_OK;
        }
        #endregion

        #region IVsENCRebuildableProjectCfg4 Implementation
        public int HasCustomMetadataEmitter([MarshalAs(UnmanagedType.VariantBool)] out bool value)
        {
            value = true;
            return _host.ENCProjectConfig4?.HasCustomMetadataEmitter(out value) ?? VSConstants.S_OK;
        }
        #endregion
    }
}