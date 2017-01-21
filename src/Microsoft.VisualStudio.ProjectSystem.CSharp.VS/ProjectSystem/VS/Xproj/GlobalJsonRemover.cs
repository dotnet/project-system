// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    internal class GlobalJsonRemover : IVsSolutionEvents
    {
        private readonly IServiceProvider _serviceProvider;

        public GlobalJsonRemover(IServiceProvider serviceProvider)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            _serviceProvider = serviceProvider;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            UIThreadHelper.VerifyOnUIThread();
            var dte = _serviceProvider.GetService<DTE2, DTE>();
            var solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
            try
            {
                Verify.HResult(solution.GetSolutionInfo(out string directory, out string solutionFile, out string optsFile));
                ProjectItem globalJson = dte.Solution.FindProjectItem(Path.Combine(directory, "global.json"));
                globalJson?.Remove();
                return VSConstants.S_OK;
            }
            finally
            {
                Verify.HResult(solution.UnadviseSolutionEvents(SolutionCookie));
            }
        }

        public uint SolutionCookie { get; set; } = VSConstants.VSCOOKIE_NIL;

        #region Unused
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }
        #endregion
    }
}
