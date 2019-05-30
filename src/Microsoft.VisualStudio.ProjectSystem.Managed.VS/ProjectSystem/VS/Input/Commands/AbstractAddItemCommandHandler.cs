// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Provides support for all Add Item commands that operate on <see cref="IProjectTree"/> nodes, across C# and VB
    /// </summary>
    internal abstract partial class AbstractAddItemCommandHandler : IAsyncCommandGroupHandler
    {
        protected static readonly Guid LegacyCSharpPackageGuid = new Guid("{FAE04EC1-301F-11d3-BF4B-00C04F79EFBC}");
        protected static readonly Guid LegacyVBPackageGuid = new Guid("{164B10B9-B200-11d0-8C61-00A0C91E29D5}");
        private readonly ConfiguredProject _configuredProject;
        private readonly IPhysicalProjectTree _projectTree;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IVsUIService<IVsAddProjectItemDlg> _addItemDialog;
        private readonly IVsUIService<IVsShell> _vsShell;

        public AbstractAddItemCommandHandler(ConfiguredProject configuredProject, IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog, IVsUIService<SVsShell, IVsShell> vsShell)
        {
            _configuredProject = configuredProject;
            _projectTree = projectTree;
            _projectVsServices = projectVsServices;
            _addItemDialog = addItemDialog;
            _vsShell = vsShell;
        }

        /// <summary>
        /// Gets the list of potential templates that could apply to this handler. Implementors should cache the results of this method.
        /// </summary>
        protected abstract ImmutableDictionary<long, ImmutableArray<TemplateDetails>> GetTemplateDetails();

        public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            Requires.NotNull(nodes, nameof(nodes));

            if (nodes.Count == 1 && _projectTree.NodeCanHaveAdditions(nodes.First()) && TryGetTemplateDetails(commandId, out _))
            {
                return GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled);
            }

            return GetCommandStatusResult.Unhandled;
        }

        public async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            Requires.NotNull(nodes, nameof(nodes));

            // We only support single node selections
            if (nodes.Count != 1)
            {
                return false;
            }

            IProjectTree node = nodes.First();

            // We only support nodes that can actually have things added to it
            if (!_projectTree.NodeCanHaveAdditions(node))
            {
                return false;
            }

            if (TryGetTemplateDetails(commandId, out TemplateDetails result))
            {
                __VSADDITEMFLAGS uiFlags = __VSADDITEMFLAGS.VSADDITEM_AddNewItems |
                                           __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName |
                                           __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView;

                string strBrowseLocations = _projectTree.TreeProvider.GetAddNewItemDirectory(node);

                await _projectVsServices.ThreadingService.SwitchToUIThread();

                // Look up the resources from each package to get the strings to pass to the Add Item dialog.
                // These strings must match what is used in the template exactly, including localized versions. Rather than relying on
                // our localizations being the same as the VS repository localizations we just load the right strings using the same
                // resource IDs as the templates themselves use.
                string dirName = _vsShell.Value.LoadPackageString(result.DirNamePackageGuid, result.DirNameResourceId);
                string templateName = _vsShell.Value.LoadPackageString(result.TemplateNamePackageGuid, result.TemplateNameResourceId);

                string strFilter = string.Empty;
                Guid addItemTemplateGuid = Guid.Empty;  // Let the dialog ask the hierarchy itself
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

            return false;
        }

        private bool TryGetTemplateDetails(long commandId, out TemplateDetails result)
        {
            IProjectCapabilitiesScope capabilities = _configuredProject.Capabilities;

            if (GetTemplateDetails().TryGetValue(commandId, out ImmutableArray<TemplateDetails> templates))
            {
                foreach (TemplateDetails template in templates)
                {
                    if (capabilities.AppliesTo(template.AppliesTo))
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
