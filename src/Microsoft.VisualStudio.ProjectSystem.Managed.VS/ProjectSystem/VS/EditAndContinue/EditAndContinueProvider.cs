// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

// We use the version of IVsENCRebuildableProjectCfg2/IVsENCRebuildableProjectCfg4 from Roslyn because the one in the SDK is defined wrong
using ENC_BREAKSTATE_REASON = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop.ENC_BREAKSTATE_REASON;
using IVsENCRebuildableProjectCfg2 = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop.IVsENCRebuildableProjectCfg2;
using IVsENCRebuildableProjectCfg4 = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop.IVsENCRebuildableProjectCfg4;

namespace Microsoft.VisualStudio.ProjectSystem.VS.EditAndContinue
{
    [ExportProjectNodeComService(typeof(IVsENCRebuildableProjectCfg), typeof(IVsENCRebuildableProjectCfg2), typeof(IVsENCRebuildableProjectCfg4))]
    [AppliesTo(ProjectCapability.EditAndContinue)]
    internal class EditAndContinueProvider : IVsENCRebuildableProjectCfg, IVsENCRebuildableProjectCfg2, IVsENCRebuildableProjectCfg4, IDisposable
    {
        private IActiveWorkspaceProjectContextHost _projectContextHost;
        private IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public EditAndContinueProvider(IActiveWorkspaceProjectContextHost projectContextHost, IProjectThreadingService threadingService)
        {
            _projectContextHost = projectContextHost;
            _threadingService = threadingService;
        }

        public int StartDebuggingPE()
        {
            return Invoke(encProvider => encProvider.StartDebuggingPE());
        }

        public int EnterBreakStateOnPE(ENC_BREAKSTATE_REASON encBreakReason, ENC_ACTIVE_STATEMENT[] pActiveStatements, uint cActiveStatements)
        {
            return Invoke(encProvider => encProvider.EnterBreakStateOnPE(encBreakReason, pActiveStatements, cActiveStatements));
        }

        public int BuildForEnc(object pUpdatePE)
        {
            return Invoke(encProvider => encProvider.BuildForEnc(pUpdatePE));
        }

        public int ExitBreakStateOnPE()
        {
            return Invoke(encProvider => encProvider.ExitBreakStateOnPE());
        }

        public int StopDebuggingPE()
        {
            return Invoke(encProvider => encProvider.StopDebuggingPE());
        }

        public int GetENCBuildState(ENC_BUILD_STATE[] pENCBuildState)
        {
            return Invoke(encProvider => encProvider.GetENCBuildState(pENCBuildState));
        }

        public int GetCurrentActiveStatementPosition(uint id, TextSpan[] ptsNewPosition)
        {
            return Invoke(encProvider => encProvider.GetCurrentActiveStatementPosition(id, ptsNewPosition));
        }

        public int GetPEidentity(Guid[] pMVID, string[] pbstrPEName)
        {
            return Invoke(encProvider => encProvider.GetPEidentity(pMVID, pbstrPEName));
        }

        public int GetExceptionSpanCount(out uint pcExceptionSpan)
        {
            uint pcExceptionSpanResult = 0;
            HResult hr = Invoke(encProvider => encProvider.GetExceptionSpanCount(out pcExceptionSpanResult));

            pcExceptionSpan = pcExceptionSpanResult;

            return hr;
        }

        public int GetExceptionSpans(uint celt, ENC_EXCEPTION_SPAN[] rgelt, ref uint pceltFetched)
        {
            uint pceltFetchedResult = pceltFetched;
            HResult hr = Invoke(encProvider => encProvider.GetExceptionSpans(celt, rgelt, ref pceltFetchedResult));

            pceltFetched = pceltFetchedResult;

            return hr;
        }

        public int GetCurrentExceptionSpanPosition(uint id, TextSpan[] ptsNewPosition)
        {
            return Invoke(encProvider => encProvider.GetCurrentExceptionSpanPosition(id, ptsNewPosition));
        }

        public int EncApplySucceeded(int hrApplyResult)
        {
            return Invoke(encProvider => encProvider.EncApplySucceeded(hrApplyResult));
        }

        public int GetPEBuildTimeStamp(OLE.Interop.FILETIME[] pTimeStamp)
        {
            return Invoke(encProvider => encProvider.GetPEBuildTimeStamp(pTimeStamp));
        }

        public int HasCustomMetadataEmitter(out bool value)
        {
            bool valueResult = false;
            HResult hr = Invoke(encProvider => ((IVsENCRebuildableProjectCfg4)encProvider).HasCustomMetadataEmitter(out valueResult));

            value = valueResult;

            return hr;
        }

        private int Invoke(Func<IVsENCRebuildableProjectCfg2, HResult> action)
        {
            return _threadingService.ExecuteSynchronously(async () =>
            {
                await _threadingService.SwitchToUIThread();

                return await _projectContextHost.OpenContextForWriteAsync(accessor =>
                {
                    return Task.FromResult(action((IVsENCRebuildableProjectCfg2)accessor.HostSpecificEditAndContinueService));
                });
            });
        }

        public void Dispose()
        {
            // Important for ProjectNodeComServices to null out fields to reduce the amount 
            // of data we leak when extensions incorrectly holds onto the IVsHierarchy.
            _projectContextHost = null;
            _threadingService = null;
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
