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
        private readonly IVsUIService<IVsShell> _vsShell;
        private readonly Lazy<Dictionary<long, List<TemplateDetails>>> _commandMap;

        [ImportingConstructor]
        public AbstractAddItemCommandHandler(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog, IVsUIService<SVsShell, IVsShell> vsShell)
        {
            _projectTree = projectTree;
            _projectVsServices = projectVsServices;
            _addItemDialog = addItemDialog;
            _vsShell = vsShell;

            _commandMap = new Lazy<Dictionary<long, List<TemplateDetails>>>(() => GetTemplateDetails());
        }

        protected abstract Dictionary<long, List<TemplateDetails>> GetTemplateDetails();

        public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            Requires.NotNull(nodes, nameof(nodes));

            if (nodes.Count == 1 && _projectTree.NodeCanHaveAdditions(nodes.First()) && TryGetCommandDetails(commandId, out _))
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
                if (TryGetCommandDetails(commandId, out TemplateDetails result))
                {
                    IProjectTree node = nodes.First();

                    if (!_projectTree.NodeCanHaveAdditions(node))
                    {
                        return false;
                    }

                    __VSADDITEMFLAGS uiFlags = __VSADDITEMFLAGS.VSADDITEM_AddNewItems |
                                               __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName |
                                               __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView;

                    string strBrowseLocations = _projectTree.TreeProvider.GetAddNewItemDirectory(node);
                    string strFilter = string.Empty;
                    Guid addItemTemplateGuid = Guid.Empty;  // Let the dialog ask the hierarchy itself

                    await _projectVsServices.ThreadingService.SwitchToUIThread();

                    // Get the strings from the legacy package
                    ErrorHandler.ThrowOnFailure(_vsShell.Value.LoadPackageString(ref result.DirNamePackageGuid, result.DirNameResourceId, out string dirName));
                    ErrorHandler.ThrowOnFailure(_vsShell.Value.LoadPackageString(ref result.TemplateNamePackageGuid, result.TemplateNameResourceId, out string templateName));

                    HResult res = _addItemDialog.Value.AddProjectItemDlg(node.GetHierarchyId(),
                                                                        ref addItemTemplateGuid,
                                                                        _projectVsServices.VsProject,
                                                                        (uint)uiFlags,
                                                                        dirName,
                                                                        templateName,
                                                                        ref strBrowseLocations,
                                                                        ref strFilter,
                                                                        out _);

                    // Return true here regardless of whether or not the user clicked OK or they clicked Cancel. This ensures that some other
                    // handler isn't called after we run.
                    return res == HResult.OK || res == HResult.Ole.PromptSaveCancelled;
                }
            }

            return false;
        }

        private bool TryGetCommandDetails(long commandId, out TemplateDetails result)
        {
            IProjectCapabilitiesScope capabilities = _projectVsServices.ActiveConfiguredProject.Capabilities;

            if (_commandMap.Value.TryGetValue(commandId, out List<TemplateDetails> templates))
            {
                foreach (TemplateDetails template in templates)
                {
                    if (capabilities.AppliesTo(template.CapabilityCheck))
                    {
                        result = template;
                        return true;
                    }
                }
            }
            result = null;
            return false;
        }
    }
}
