// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS;
using EncInterop = Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Managed.VS.EditAndContinue
{

    [ExportProjectNodeComService(typeof(IVsENCRebuildableProjectCfg))]
    [AppliesTo(ProjectCapability.EditAndContinue)]
    internal class EditAndContinueProvider : IVsENCRebuildableProjectCfg
    {
        private readonly ILanguageServiceHost _host;

        [ImportingConstructor]
        public EditAndContinueProvider(ILanguageServiceHost host)
        {
            _host = host;
        }

        // AbstractProject implements IVsENCRebuildableProjectCfg2 and IVsENCRebuildableProjectCfg4 only
        [ExportProjectNodeComService(typeof(EncInterop.IVsENCRebuildableProjectCfg2), typeof(EncInterop.IVsENCRebuildableProjectCfg4))]
        [AppliesTo(ProjectCapability.EditAndContinue)]
        internal Object EditAndContinueService
        {
            get
            {
                return _host.HostSpecificEditAndContinueService;
            }
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