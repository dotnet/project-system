// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Shell
{
    /// <summary>
    ///     Provides operations for manipulating nodes in Solution Explorer.
    /// </summary>
    [Export]
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal class SolutionExplorerWindow
    {
        private static readonly Guid s_solutionExplorer = new(EnvDTE.Constants.vsWindowKindSolutionExplorer);

        private readonly Lazy<IVsUIHierarchyWindow2?> _solutionExplorer;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public SolutionExplorerWindow(
            IProjectThreadingService threadingService,
            IVsUIService<SVsUIShell, IVsUIShell> shell)
        {
            _solutionExplorer = new Lazy<IVsUIHierarchyWindow2?>(() => GetUIHierarchyWindow(shell, s_solutionExplorer));
            _threadingService = threadingService;
        }

        /// <summary>
        ///     Gets a value indicating if the Solution Explorer window is available.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     This property was not accessed from the UI thread.
        /// </exception>
        public bool IsAvailable
        {
            get
            {
                _threadingService.VerifyOnUIThread();
                return _solutionExplorer.Value is not null;
            }
        }

        /// <summary>
        ///     Selects the specified node.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="uiHierarchy"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="id"/> is nil, empty or represents a selection.</exception>
        /// <exception cref="InvalidOperationException">This method was not accessed from the UI thread.</exception>
        /// <exception cref="InvalidOperationException"><see cref="IsAvailable"/> is <see langword="false"/>.</exception>
        public HResult Select(IVsUIHierarchy uiHierarchy, HierarchyId id)
        {
            return ExpandItem(uiHierarchy, id, EXPANDFLAGS.EXPF_SelectItem);
        }

        private HResult ExpandItem(IVsUIHierarchy uiHierarchy, HierarchyId id, EXPANDFLAGS flags)
        {
            Requires.NotNull(uiHierarchy, nameof(uiHierarchy));
            Requires.Argument(!(id.IsNilOrEmpty || id.IsSelection), nameof(id), "id must not be nil, empty or represent a selection.");

            return Invoke(window => window.ExpandItem(uiHierarchy, id, flags));
        }

        private HResult Invoke(Func<IVsUIHierarchyWindow2, HResult> action)
        {
            _threadingService.VerifyOnUIThread();

            IVsUIHierarchyWindow2? window = _solutionExplorer.Value;
            if (window is null)
            {
                throw new InvalidOperationException("Solution Explorer is not available in command-line mode.");
            }

            return action(window);
        }

        private static IVsUIHierarchyWindow2? GetUIHierarchyWindow(IVsUIService<IVsUIShell> shell, Guid persistenceSlot)
        {
            if (ErrorHandler.Succeeded(shell.Value.FindToolWindow(0, ref persistenceSlot, out IVsWindowFrame? frame)) && frame is not null)
            {
                ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object? view));

                return (IVsUIHierarchyWindow2)view;
            }

            // Command-line/non-UI mode
            return null;
        }
    }
}
