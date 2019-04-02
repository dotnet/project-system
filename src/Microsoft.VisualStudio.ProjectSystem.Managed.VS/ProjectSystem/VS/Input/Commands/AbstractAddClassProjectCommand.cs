// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractAddClassProjectCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IVsUIService<IVsAddProjectItemDlg> _addItemDialog;

        protected abstract string DirName { get; }

        protected AbstractAddClassProjectCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog)
        {
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            Requires.NotNull(addItemDialog, nameof(addItemDialog));

            _projectTree = projectTree;
            _projectVsServices = projectVsServices;
            _addItemDialog = addItemDialog;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (_projectTree.NodeCanHaveAdditions(node))
            {
                return GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled);
            }
            else
            {
                return GetCommandStatusResult.Unhandled;
            }
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (!_projectTree.NodeCanHaveAdditions(node))
            {
                return false;
            }

            __VSADDITEMFLAGS uiFlags = __VSADDITEMFLAGS.VSADDITEM_AddNewItems | __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName | __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView;

            string strBrowseLocations = _projectTree.TreeProvider.GetAddNewItemDirectory(node);

            string strFilter = string.Empty;
            await _projectVsServices.ThreadingService.SwitchToUIThread();

            Guid addItemTemplateGuid = Guid.Empty;  // Let the dialog ask the hierarchy itself
            HResult res = _addItemDialog.Value.AddProjectItemDlg(node.GetHierarchyId(), ref addItemTemplateGuid, _projectVsServices.VsProject, (uint)uiFlags,
                DirName, VSResources.ClassTemplateName, ref strBrowseLocations, ref strFilter, out _);

            // Return true here regardless of whether or not the user clicked OK or they clicked Cancel. This ensures that some other
            // handler isn't called after we run.
            return res == VSConstants.S_OK || res == VSConstants.OLE_E_PROMPTSAVECANCELLED;
        }
    }
}
