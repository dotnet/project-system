// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractMoveCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly ConfiguredProject _configuredProject;

        public AbstractMoveCommand(IPhysicalProjectTree projectTree, SVsServiceProvider serviceProvider, ConfiguredProject configuredProject)
        {
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(configuredProject, nameof(configuredProject));

            _projectTree = projectTree;
            _serviceProvider = serviceProvider;
            _configuredProject = configuredProject;
        }

        protected abstract bool CanMove(IProjectTree node);

        protected abstract Task<bool> TryMoveAsync(ConfiguredProject configuredProject, IProjectTree node);

        protected override async Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (!CanMove(node))
            {
                return await GetCommandStatusResult.Handled(commandText, CommandStatus.Ninched).ConfigureAwait(true);
            }

            return await GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled).ConfigureAwait(true);
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            var didMove = await TryMoveAsync(_configuredProject, node).ConfigureAwait(true);

            if (didMove)
            {
                // Wait for updating to finish before re-selecting the node that moved.
                // We need to re-select the node after it is moved in order to continuously move the node using hotkeys.
                await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);

                if (_configuredProject.UnconfiguredProject.Services.HostObject is IVsUIHierarchy hierarchy)
                {
                    // The node moved will retain its identity.
                    var itemId = (uint)node.Identity.ToInt32();
                    var window = GetUIHierarchyWindow(_serviceProvider, Guid.Parse(ManagedProjectSystemPackage.SolutionExplorerGuid));

                    window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_SelectItem);
                    
                    // If we moved a folder, collapse it.
                    if (node.IsFolder)
                    {
                        window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_CollapseFolder);
                    }
                }
            }

            return didMove;
        }

        /// <summary>
        /// Get reference to IVsUIHierarchyWindow interface from guid persistence slot.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="persistenceSlot">Unique identifier for a tool window created using IVsUIShell::CreateToolWindow.
        /// The caller of this method can use predefined identifiers that map to tool windows if those tool windows
        /// are known to the caller. </param>
        /// <returns>A reference to an IVsUIHierarchyWindow interface, or <c>null</c> if the window isn't available, such as command line mode.</returns>
        private static IVsUIHierarchyWindow GetUIHierarchyWindow(IServiceProvider serviceProvider, Guid persistenceSlot)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            IVsUIShell shell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;

            if (shell == null)
            {
                throw new InvalidOperationException();
            }

            object pvar = null;
            IVsUIHierarchyWindow uiHierarchyWindow = null;

            try
            {
                if (ErrorHandler.Succeeded(shell.FindToolWindow(0, ref persistenceSlot, out var frame)) && frame != null)
                {
                    ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out pvar));
                }
            }
            finally
            {
                if (pvar != null)
                {
                    uiHierarchyWindow = (IVsUIHierarchyWindow)pvar;
                }
            }

            return uiHierarchyWindow;
        }
    }
}
