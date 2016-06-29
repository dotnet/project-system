// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(CommandGroup.VisualStudioStandard97, 946)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class AddClassProjectCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        public AddClassProjectCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider)
        {
            _projectTree = projectTree;
            _projectVsServices = projectVsServices;
            _serviceProvider = serviceProvider;
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


            string strBrowseLocations = _projectTree.TreeProvider.GetAddNewItemDirectory(node);
            IVsAddProjectItemDlg addItemDialog = _serviceProvider.GetService<IVsAddProjectItemDlg, SVsAddProjectItemDlg>();
            Assumes.Present(addItemDialog);

            string strFilter = string.Empty;
            int iDontShowAgain;

            ConfigurationGeneral projectProperties = await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            Guid guid = new Guid((string)await projectProperties.ProjectGuid.GetValueAsync().ConfigureAwait(false));

            __VSADDITEMFLAGS uiFlags = __VSADDITEMFLAGS.VSADDITEM_AddNewItems | __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName | __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView;

            await _projectVsServices.ThreadingService.SwitchToUIThread();

            var res = addItemDialog.AddProjectItemDlg(node.GetHierarchyId().Id, ref guid, _projectVsServices.VsProject, (uint)uiFlags, Resources.CSharpItemsDirName, Resources.ClassTemplateName, ref strBrowseLocations, ref strFilter, out iDontShowAgain);

            // Don't show it again if the user clicked cancel.
            return res == VSConstants.S_OK || res == VSConstants.OLE_E_PROMPTSAVECANCELLED;
        }
    }
}
