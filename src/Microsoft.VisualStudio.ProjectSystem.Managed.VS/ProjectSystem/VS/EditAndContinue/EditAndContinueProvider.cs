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
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).BuildForEnc(pUpdatePE);
        }

        public int EncApplySucceeded([In] int hrApplyResult)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).EncApplySucceeded(hrApplyResult);
        }

        public int EnterBreakStateOnPE([In] EncInterop.ENC_BREAKSTATE_REASON encBreakReason, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ENC_ACTIVE_STATEMENT[] pActiveStatements, [In] uint cActiveStatements)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).EnterBreakStateOnPE(encBreakReason, pActiveStatements, cActiveStatements);
        }

        public int ExitBreakStateOnPE()
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).ExitBreakStateOnPE();
        }

        public int GetCurrentActiveStatementPosition([In] uint id, [MarshalAs(UnmanagedType.LPArray), Out] TextSpan[] ptsNewPosition)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).GetCurrentActiveStatementPosition(id, ptsNewPosition);
        }

        public int GetCurrentExceptionSpanPosition([In] uint id, [MarshalAs(UnmanagedType.LPArray), Out] TextSpan[] ptsNewPosition)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).GetCurrentExceptionSpanPosition(id, ptsNewPosition);
        }

        public int GetENCBuildState([MarshalAs(UnmanagedType.LPArray), Out] ENC_BUILD_STATE[] pENCBuildState)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).GetENCBuildState(pENCBuildState);
        }

        public int GetExceptionSpanCount([Out] out uint pcExceptionSpan)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).GetExceptionSpanCount(out pcExceptionSpan);
        }

        public int GetExceptionSpans([In] uint celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] ENC_EXCEPTION_SPAN[] rgelt, [In, Out] ref uint pceltFetched)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).GetExceptionSpans(celt, rgelt, ref pceltFetched);
        }

        public int GetPEBuildTimeStamp([MarshalAs(UnmanagedType.LPArray), Out] OLE.Interop.FILETIME[] pTimeStamp)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).GetPEBuildTimeStamp(pTimeStamp);
        }

        public int GetPEidentity([MarshalAs(UnmanagedType.LPArray), Out] Guid[] pMVID, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr), Out] string[] pbstrPEName)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).GetPEidentity(pMVID, pbstrPEName);
        }

        public int StartDebuggingPE()
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).StartDebuggingPE();
        }

        public int StopDebuggingPE()
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_host.ActiveProjectContext).StopDebuggingPE();
        }
        #endregion

        #region IVsENCRebuildableProjectCfg4 Implementation
        public int HasCustomMetadataEmitter([MarshalAs(UnmanagedType.VariantBool)] out bool value)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg4)_host.ActiveProjectContext).HasCustomMetadataEmitter(out value);
        }
        #endregion
    }
}