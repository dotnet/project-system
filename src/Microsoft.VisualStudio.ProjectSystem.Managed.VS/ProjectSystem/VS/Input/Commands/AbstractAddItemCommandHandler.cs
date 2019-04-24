// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Provides support for all Add Item commands that operate on <see cref="IProjectTree"/> nodes, across C# and VB
    /// </summary>
    internal abstract partial class AbstractAddItemCommandHandler : IAsyncCommandGroupHandler
    {
        protected static readonly Guid LegacyCSharpPackageGuid = new Guid("{FAE04EC1-301F-11d3-BF4B-00C04F79EFBC}");
        protected static readonly Guid LegacyVBPackageGuid = new Guid("{164B10B9-B200-11d0-8C61-00A0C91E29D5}");

        private readonly IPhysicalProjectTree _projectTree;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IVsUIService<IVsAddProjectItemDlg> _addItemDialog;
        private readonly IVsService<SVsShell, IVsShell> _vsShell;
        private readonly Lazy<Dictionary<long, CommandDetails>> _cSharpCommandMap;
        private readonly Lazy<Dictionary<long, CommandDetails>> _vbCommandMap;

        [ImportingConstructor]
        public AbstractAddItemCommandHandler(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog, IVsService<SVsShell, IVsShell> vsShell)
        {
            _projectTree = projectTree;
            _projectVsServices = projectVsServices;
            _addItemDialog = addItemDialog;
            _vsShell = vsShell;

            _cSharpCommandMap = new Lazy<Dictionary<long, CommandDetails>>(() => GetCSharpCommands());
            _vbCommandMap = new Lazy<Dictionary<long, CommandDetails>>(() => GetVBCommands());
        }

        protected abstract Dictionary<long, CommandDetails> GetCSharpCommands();
        protected abstract Dictionary<long, CommandDetails> GetVBCommands();

        public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            Requires.NotNull(nodes, nameof(nodes));

            if (TryGetCommandDetails(commandId, out _))
            {
                return GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled);
            }

            return GetCommandStatusResult.Unhandled;
        }

        public async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            Requires.NotNull(nodes, nameof(nodes));

            if (nodes.Count == 1)
            {
                if (TryGetCommandDetails(commandId, out CommandDetails result))
                {
                    IProjectTree node = nodes.First();

                    // Get the strings from the legacy package
                    IVsShell vsShell = await _vsShell.GetValueAsync();
                    ErrorHandler.ThrowOnFailure(vsShell.LoadPackageString(ref result.DirNamePackageGuid, result.DirNameResourceId, out string dirName));
                    ErrorHandler.ThrowOnFailure(vsShell.LoadPackageString(ref result.TemplateNamePackageGuid, result.TemplateNameResourceId, out string templateName));

                    return await ShowAddProjectItemDialog(node, dirName, templateName, _projectTree, _projectVsServices, _addItemDialog);
                }
            }

            return false;
        }

        private bool TryGetCommandDetails(long commandId, out CommandDetails result)
        {
            IProjectCapabilitiesScope capabilities = _projectVsServices.ActiveConfiguredProject.Capabilities;

            if (_cSharpCommandMap.Value.TryGetValue(commandId, out result) && capabilities.AppliesTo(ProjectCapability.CSharp))
            {
                return true;
            }
            else if (_vbCommandMap.Value.TryGetValue(commandId, out result) && capabilities.AppliesTo(ProjectCapability.VisualBasic))
            {
                return true;
            }
            return false;
        }

        internal static async Task<bool> ShowAddProjectItemDialog(IProjectTree node, string dirName, string templateName, IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServcies, IVsUIService<IVsAddProjectItemDlg> addItemDialog)
        {
            Requires.NotNull(node, nameof(node));
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(projectVsServcies, nameof(projectVsServcies));
            Requires.NotNull(addItemDialog, nameof(addItemDialog));

            if (!projectTree.NodeCanHaveAdditions(node))
            {
                return false;
            }

            __VSADDITEMFLAGS uiFlags = __VSADDITEMFLAGS.VSADDITEM_AddNewItems |
                                       __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName |
                                       __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView;

            string strBrowseLocations = projectTree.TreeProvider.GetAddNewItemDirectory(node);
            string strFilter = string.Empty;
            Guid addItemTemplateGuid = Guid.Empty;  // Let the dialog ask the hierarchy itself

            await projectVsServcies.ThreadingService.SwitchToUIThread();

            HResult res = addItemDialog.Value.AddProjectItemDlg(node.GetHierarchyId(),
                                                                ref addItemTemplateGuid,
                                                                projectVsServcies.VsProject,
                                                                (uint)uiFlags,
                                                                dirName,
                                                                templateName,
                                                                ref strBrowseLocations,
                                                                ref strFilter,
                                                                out _);

            // Return true here regardless of whether or not the user clicked OK or they clicked Cancel. This ensures that some other
            // handler isn't called after we run.
            return res == VSConstants.S_OK || res == VSConstants.OLE_E_PROMPTSAVECANCELLED;
        }
    }
}
