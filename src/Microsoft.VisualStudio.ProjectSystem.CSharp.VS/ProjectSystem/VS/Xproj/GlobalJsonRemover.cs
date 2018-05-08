// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    internal class GlobalJsonRemover : IVsSolutionEvents
    {
        /// <summary>
        /// Static GlobalJsonRemover instance. All interactions must occur on the UI thread.
        /// </summary>
        private static GlobalJsonRemover s_remover;

        /// <summary>
        /// Testing-only getter/setter
        /// </summary>
        internal static GlobalJsonRemover Remover
        {
            get
            {
                Assumes.True(UnitTestHelper.IsRunningUnitTests);
                return s_remover;
            }
            set
            {
                Assumes.True(UnitTestHelper.IsRunningUnitTests);
                s_remover = value;
            }
        }

        /// <summary>
        /// Helper class for testing purposes, so we can verify calls.
        /// </summary>
        public class GlobalJsonSetup
        {
            /// <summary>
            /// Initializes the <see cref="GlobalJsonRemover"/> if not already initialized. This method assumes that it will be called
            /// from the UI thread, and will throw if this isn't true.
            /// </summary>
            /// <returns>True if the remover was set up for the first time. False otherwise.</returns>
            public virtual bool SetupRemoval(IVsSolution solution, IServiceProvider provider, IFileSystem fileSystem)
            {
                UIThreadHelper.VerifyOnUIThread();
                if (s_remover != null)
                    return false;

                s_remover = new GlobalJsonRemover(provider, fileSystem);
                Verify.HResult(solution.AdviseSolutionEvents(s_remover, out uint cookie));
                s_remover.SolutionCookie = cookie;
                return true;
            }
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly IFileSystem _fileSystem;

        internal GlobalJsonRemover(IServiceProvider serviceProvider, IFileSystem fileSystem)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(fileSystem, nameof(fileSystem));
            _serviceProvider = serviceProvider;
            _fileSystem = fileSystem;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            UIThreadHelper.VerifyOnUIThread();
            DTE2 dte = _serviceProvider.GetService<DTE2, DTE>();
            IVsSolution solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
            try
            {
                Verify.HResult(solution.GetSolutionInfo(out string directory, out string solutionFile, out string optsFile));
                string globalJsonPath = Path.Combine(directory, "global.json");
                ProjectItem globalJson = dte.Solution.FindProjectItem(globalJsonPath);
                globalJson?.Delete();
                try
                {
                    _fileSystem.RemoveFile(globalJsonPath);
                }
                catch (FileNotFoundException) { }
                return VSConstants.S_OK;
            }
            finally
            {
                Verify.HResult(solution.UnadviseSolutionEvents(SolutionCookie));
                // Don't keep a static reference around to an object that won't be used again.
                Assumes.True(ReferenceEquals(this, s_remover));
                s_remover = null;
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
