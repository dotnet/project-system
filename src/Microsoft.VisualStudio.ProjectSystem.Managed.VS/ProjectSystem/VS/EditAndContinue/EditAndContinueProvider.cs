// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

// We use the version of IVsENCRebuildableProjectCfg2/IVsENCRebuildableProjectCfg4 from Roslyn because the one in the SDK is defined wrong
using IVsENCRebuildableProjectCfg2 = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop.IVsENCRebuildableProjectCfg2;
using IVsENCRebuildableProjectCfg4 = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop.IVsENCRebuildableProjectCfg4;
using ENC_BREAKSTATE_REASON = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop.ENC_BREAKSTATE_REASON;

namespace Microsoft.VisualStudio.ProjectSystem.VS.EditAndContinue
{
    [ExportProjectNodeComService(typeof(IVsENCRebuildableProjectCfg), typeof(IVsENCRebuildableProjectCfg2), typeof(IVsENCRebuildableProjectCfg4))]
    [AppliesTo(ProjectCapability.EditAndContinue)]
    internal class EditAndContinueProvider : IVsENCRebuildableProjectCfg, IVsENCRebuildableProjectCfg2, IVsENCRebuildableProjectCfg4, IDisposable
    {
        private ILanguageServiceHost _host;

        [ImportingConstructor]
        public EditAndContinueProvider(ILanguageServiceHost host)
        {
            _host = host;
        }

        public int StartDebuggingPE()
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.StartDebuggingPE();
            }

            return HResult.Unexpected;
        }

        public int EnterBreakStateOnPE(ENC_BREAKSTATE_REASON encBreakReason, ENC_ACTIVE_STATEMENT[] pActiveStatements, uint cActiveStatements)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.EnterBreakStateOnPE(encBreakReason, pActiveStatements, cActiveStatements);
            }

            return HResult.Unexpected;
        }

        public int BuildForEnc(object pUpdatePE)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.BuildForEnc(pUpdatePE);
            }

            return HResult.Unexpected;
        }

        public int ExitBreakStateOnPE()
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.ExitBreakStateOnPE();
            }

            return HResult.Unexpected;
        }

        public int StopDebuggingPE()
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.StopDebuggingPE();
            }

            return HResult.Unexpected;
        }

        public int GetENCBuildState(ENC_BUILD_STATE[] pENCBuildState)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.GetENCBuildState(pENCBuildState);
            }

            return HResult.Unexpected;
        }

        public int GetCurrentActiveStatementPosition(uint id, TextSpan[] ptsNewPosition)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.GetCurrentActiveStatementPosition(id, ptsNewPosition);
            }

            return HResult.Unexpected;
        }

        public int GetPEidentity(Guid[] pMVID, string[] pbstrPEName)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.GetPEidentity(pMVID, pbstrPEName);
            }

            return HResult.Unexpected;
        }

        public int GetExceptionSpanCount(out uint pcExceptionSpan)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.GetExceptionSpanCount(out pcExceptionSpan);
            }

            pcExceptionSpan = 0;
            return HResult.Unexpected;
        }

        public int GetExceptionSpans(uint celt, ENC_EXCEPTION_SPAN[] rgelt, ref uint pceltFetched)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.GetExceptionSpans(celt, rgelt, pceltFetched);
            }

            return HResult.Unexpected;
        }

        public int GetCurrentExceptionSpanPosition(uint id, TextSpan[] ptsNewPosition)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.GetCurrentExceptionSpanPosition(id, ptsNewPosition);
            }

            return HResult.Unexpected;
        }

        public int EncApplySucceeded(int hrApplyResult)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.EncApplySucceeded(hrApplyResult);
            }

            return HResult.Unexpected;
        }

        public int GetPEBuildTimeStamp(OLE.Interop.FILETIME[] pTimeStamp)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg2 encProvider)
            {
                return encProvider.GetPEBuildTimeStamp(pTimeStamp);
            }

            return HResult.Unexpected;
        }

        public int HasCustomMetadataEmitter(out bool value)
        {
            if (_host?.HostSpecificEditAndContinueService is IVsENCRebuildableProjectCfg4 encProvider)
            {
                return encProvider.HasCustomMetadataEmitter(out value);
            }

            value = false;
            return HResult.Unexpected;
        }

        public void Dispose()
        {
            // Important for ProjectNodeComServices to null out fields to reduce the amount 
            // of data we leak when extensions incorrectly holds onto the IVsHierarchy.
            _host = null;
        }

        // NOTE: Managed ENC always calls through IVsENCRebuildableProjectCfg2/IVsENCRebuildableProjectCfg4.
        // We implement IVsENCRebuildableProjectCfg as this used to sniff the project for EnC support.
        int IVsENCRebuildableProjectCfg.ENCRebuild(object in_pProgram, out object out_ppSnapshot)
        {
            out_ppSnapshot = null;
            return HResult.NotImplemented;
        }

        int IVsENCRebuildableProjectCfg.BelongToProject(string in_szFileName, ENC_REASON in_ENCReason, int in_fOnContinue)
        {
            return HResult.NotImplemented;
        }

        int IVsENCRebuildableProjectCfg.ENCComplete(int in_fENCSuccess)
        {
            return HResult.NotImplemented;
        }

        int IVsENCRebuildableProjectCfg.CancelENC()
        {
            return HResult.NotImplemented;
        }

        int IVsENCRebuildableProjectCfg.ENCRelink([In, MarshalAs(UnmanagedType.IUnknown)] object pENCRelinkInfo)
        {
            return HResult.NotImplemented;
        }

        int IVsENCRebuildableProjectCfg.StartDebugging()
        {
            return HResult.NotImplemented;
        }

        int IVsENCRebuildableProjectCfg.StopDebugging()
        {
            return HResult.NotImplemented;
        }

        int IVsENCRebuildableProjectCfg.SetENCProjectBuildOption([In] ref Guid in_guidOption, [In, MarshalAs(UnmanagedType.LPWStr)] string in_szOptionValue)
        {
            return HResult.NotImplemented;
        }
    }
}
