// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractAddClassProjectCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly SVsServiceProvider _serviceProvider;

        protected abstract string DirName { get; }

        public AbstractAddClassProjectCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider)
        {
            Requires.NotNull(projectTree, nameof(IPhysicalProjectTree));
            Requires.NotNull(projectVsServices, nameof(IUnconfiguredProjectVsServices));
            Requires.NotNull(serviceProvider, nameof(SVsServiceProvider));

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

            ConfigurationGeneral projectProperties =
                            await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            Guid guid = new Guid((string)await projectProperties.ProjectGuid.GetValueAsync().ConfigureAwait(false));

            __VSADDITEMFLAGS uiFlags = __VSADDITEMFLAGS.VSADDITEM_AddNewItems | __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName | __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView;

            string strBrowseLocations = _projectTree.TreeProvider.GetAddNewItemDirectory(node);

            string strFilter = string.Empty;
            int iDontShowAgain;

            await _projectVsServices.ThreadingService.SwitchToUIThread();

            IVsAddProjectItemDlg addItemDialog = _serviceProvider.GetService<IVsAddProjectItemDlg, SVsAddProjectItemDlg>();
            Assumes.Present(addItemDialog);
            HResult res = addItemDialog.AddProjectItemDlg(node.GetHierarchyId(), ref guid, _projectVsServices.VsProject, (uint)uiFlags,
                DirName, VSResources.ClassTemplateName, ref strBrowseLocations, ref strFilter, out iDontShowAgain);

            // Return true here regardless of whether or not the user clicked OK or they clicked Cancel. This ensures that some other
            // handler isn't called after we run.
            return res == VSConstants.S_OK || res == VSConstants.OLE_E_PROMPTSAVECANCELLED;
        }
    }
}
