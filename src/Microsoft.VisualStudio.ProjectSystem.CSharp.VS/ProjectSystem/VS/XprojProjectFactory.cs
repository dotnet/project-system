// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Packaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Guid(CSharpProjectSystemPackage.XprojTypeGuid)]
    [Export]
    internal class XprojProjectFactory : IVsProjectFactory, IVsSolutionEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private bool _showedDialogForThisSolution = false;
        private bool _solutionLoaded = false;
        private uint _adviseCookie = 0;
        private bool _adviseRegistered = false;

        public XprojProjectFactory(IServiceProvider serviceProvider)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            _serviceProvider = serviceProvider;
        }

        public int CanCreateProject(string pszFilename, uint grfCreateFlags, out int pfCanCreate)
        {
            pfCanCreate = pszFilename.EndsWith("xproj", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            return VSConstants.S_OK;
        }

        public int CreateProject(string pszFilename, string pszLocation, string pszName, uint grfCreateFlags, ref Guid iidProject, out IntPtr ppvProject, out int pfCanceled)
        {
            ppvProject = IntPtr.Zero;
            pfCanceled = 1;

            // Only show the dialog once for all xproj's in the solution
            if (!_showedDialogForThisSolution)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (!_adviseRegistered)
                {
                    var solution = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));
                    solution.AdviseSolutionEvents(this, out _adviseCookie);
                    _adviseRegistered = true;
                }

                var uiShell = _serviceProvider.GetService(typeof(IVsUIShell)) as IVsUIShell;
                Assumes.Present(uiShell);
                if (uiShell == null)
                {
                    throw new InvalidOperationException();
                }

                Guid emptyGuid = Guid.Empty;
                int result = 0;

                ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                    0,
                    ref emptyGuid,
                    null,
                    VSResources.DotNetCoreProjectsNotSupported,
                    null,
                    0,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    OLEMSGICON.OLEMSGICON_INFO,
                    0,
                    out result));

                // Only set this to true if we aren't loaded so that all projects durring load only display one dialog. After the solution loads, the flag is cleared
                // so that r-click relaod project still shows UI
                if (!_solutionLoaded)
                {
                    _showedDialogForThisSolution = true;
                }
            }

            return VSConstants.S_OK;
        }

        public int SetSite(OLE.Interop.IServiceProvider psp)
        {
            // This never gets called
            return VSConstants.S_OK;
        }

        public int Close()
        {
            throw new NotImplementedException();
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            _showedDialogForThisSolution = false;
            _solutionLoaded = true;
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            _showedDialogForThisSolution = false;
            _solutionLoaded = false;
            return VSConstants.S_OK;
        }

        // The rest of the IVsSolutionEvents methods aren't used, just return OK

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
    }
}
