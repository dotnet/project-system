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
    class EditAndContinueProvider : IVsENCRebuildableProjectCfg, EncInterop.IVsENCRebuildableProjectCfg2, EncInterop.IVsENCRebuildableProjectCfg4
    {
        private const int E_NOTIMPL = unchecked((int)0x80004001);
        private readonly IWorkspaceProjectContext _projectContext;

        [ImportingConstructor]
        public EditAndContinueProvider(
            ILanguageServiceHost host)
        {
            _projectContext = host.ActiveProjectContext;
        }

        #region IVsENCRebuildableProjectCfg Implementation
        // Managed ENC always uses IVsENCRebuildableProjectCfg2. 
        public int ENCRebuild(object in_pProgram, out object out_ppSnapshot)
        {
            out_ppSnapshot = null;
            return E_NOTIMPL;
        }

        public int BelongToProject(string in_szFileName, ENC_REASON in_ENCReason, int in_fOnContinue)
        {
            return E_NOTIMPL;
        }

        public int ENCComplete(int in_fENCSuccess)
        {
            return E_NOTIMPL;
        }

        public int CancelENC()
        {
            return E_NOTIMPL;
        }

        public int ENCRelink([In, MarshalAs(UnmanagedType.IUnknown)] object pENCRelinkInfo)
        {
            return E_NOTIMPL;
        }

        public int StartDebugging()
        {
            return E_NOTIMPL;
        }

        public int StopDebugging()
        {
            return E_NOTIMPL;
        }

        public int SetENCProjectBuildOption([In] ref Guid in_guidOption, [In, MarshalAs(UnmanagedType.LPWStr)] string in_szOptionValue)
        {
            return E_NOTIMPL;
        }
        #endregion
        #region IVsENCRebuildableProjectCfg2 Implementation
        public int BuildForEnc([In, MarshalAs(UnmanagedType.IUnknown)] object pUpdatePE)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).BuildForEnc(pUpdatePE);
        }

        public int EncApplySucceeded([In] int hrApplyResult)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).EncApplySucceeded(hrApplyResult);
        }

        public int EnterBreakStateOnPE([In] EncInterop.ENC_BREAKSTATE_REASON encBreakReason, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ENC_ACTIVE_STATEMENT[] pActiveStatements, [In] uint cActiveStatements)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).EnterBreakStateOnPE(encBreakReason, pActiveStatements, cActiveStatements);
        }

        public int ExitBreakStateOnPE()
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).ExitBreakStateOnPE();
        }

        public int GetCurrentActiveStatementPosition([In] uint id, [MarshalAs(UnmanagedType.LPArray), Out] TextSpan[] ptsNewPosition)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).GetCurrentActiveStatementPosition(id, ptsNewPosition);
        }

        public int GetCurrentExceptionSpanPosition([In] uint id, [MarshalAs(UnmanagedType.LPArray), Out] TextSpan[] ptsNewPosition)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).GetCurrentExceptionSpanPosition(id, ptsNewPosition);
        }

        public int GetENCBuildState([MarshalAs(UnmanagedType.LPArray), Out] ENC_BUILD_STATE[] pENCBuildState)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).GetENCBuildState(pENCBuildState);
        }

        public int GetExceptionSpanCount([Out] out uint pcExceptionSpan)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).GetExceptionSpanCount(out pcExceptionSpan);
        }

        public int GetExceptionSpans([In] uint celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] ENC_EXCEPTION_SPAN[] rgelt, [In, Out] ref uint pceltFetched)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).GetExceptionSpans(celt, rgelt, ref pceltFetched);
        }

        public int GetPEBuildTimeStamp([MarshalAs(UnmanagedType.LPArray), Out] OLE.Interop.FILETIME[] pTimeStamp)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).GetPEBuildTimeStamp(pTimeStamp);
        }

        public int GetPEidentity([MarshalAs(UnmanagedType.LPArray), Out] Guid[] pMVID, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr), Out] string[] pbstrPEName)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).GetPEidentity(pMVID, pbstrPEName);
        }

        public int StartDebuggingPE()
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).StartDebuggingPE();
        }

        public int StopDebuggingPE()
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg2)_projectContext).StopDebuggingPE();
        }
        #endregion
        #region IVsENCRebuildableProjectCfg4 Implementation
        public int HasCustomMetadataEmitter([MarshalAs(UnmanagedType.VariantBool)] out bool value)
        {
            return ((EncInterop.IVsENCRebuildableProjectCfg4)_projectContext).HasCustomMetadataEmitter(out value);
        }
        #endregion
    }
}