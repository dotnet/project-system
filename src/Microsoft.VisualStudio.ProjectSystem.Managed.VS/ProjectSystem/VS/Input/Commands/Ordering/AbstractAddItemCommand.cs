// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractAddItemCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly SVsServiceProvider _serviceProvider;

        public AbstractAddItemCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider)
        {
            Requires.NotNull(projectTree, nameof(IPhysicalProjectTree));
            Requires.NotNull(projectVsServices, nameof(IUnconfiguredProjectVsServices));
            Requires.NotNull(serviceProvider, nameof(SVsServiceProvider));

            _projectTree = projectTree;
            _projectVsServices = projectVsServices;
            _serviceProvider = serviceProvider;
        }

        protected abstract bool CanAdd(IProjectTree target);

        protected abstract IProjectTree GetNodeToAddTo(IProjectTree target);

        protected abstract Task OnAddingNodesAsync(IProjectTree nodeToAddTo);

        protected virtual void OnAddedElements(Project project, ImmutableArray<ProjectItemElement> elements, IProjectTree target, IProjectTree nodeToAddTo)
        {
            OrderingHelper.TryMoveElementsToTop(project, elements, nodeToAddTo);
        }

        protected Task ShowAddNewFileDialogAsync(IProjectTree target)
        {
            return HACK_AddItemHelper.ShowAddNewFileDialogAsync(_projectTree, _projectVsServices, _serviceProvider, target);
        }

        protected Task ShowAddExistingFilesDialogAsync(IProjectTree target)
        {
            return HACK_AddItemHelper.ShowAddExistingFilesDialogAsync(_projectTree, _projectVsServices, _serviceProvider, target);
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (_projectTree.NodeCanHaveAdditions(GetNodeToAddTo(node)) && CanAdd(node))
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
            var nodeToAddTo = GetNodeToAddTo(node);

            var addedElements = await OrderingHelper.AddItems(_projectVsServices.ActiveConfiguredProject, _projectVsServices.ProjectLockService, () => OnAddingNodesAsync(nodeToAddTo)).ConfigureAwait(true);

            if (addedElements.Any())
            {
                await new ProjectAccessor(_projectVsServices.ProjectLockService).OpenProjectForWriteAsync(_projectVsServices.ActiveConfiguredProject, project => OnAddedElements(project, addedElements, node, nodeToAddTo)).ConfigureAwait(true);

                // Re-select the node that was the target.
                await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);
                HACK_NodeHelper.Select(_projectVsServices.ActiveConfiguredProject, _serviceProvider, node);

                // If the node we wanted to add to is a folder, make sure it is expanded.
                if (nodeToAddTo.IsFolder)
                {
                    HACK_NodeHelper.ExpandFolder(_projectVsServices.ActiveConfiguredProject, _serviceProvider, nodeToAddTo);
                }

                await Task.Delay(1000).ConfigureAwait(true);

                if (addedElements.Length == 1)
                {
                    var unconfiguredProject = _projectVsServices.ActiveConfiguredProject.UnconfiguredProject;
                    var filePath = unconfiguredProject.MakeRooted(addedElements[0].Include);
                    var addedNode = _projectTree.CurrentTree.FindImmediateChildByPath(filePath);

                    Assumes.NotNull(addedNode);

                    var uiShellOpenDocument = _serviceProvider.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

                    var sp = (IOleServiceProvider)_serviceProvider.GetService(typeof(IOleServiceProvider));
                    var docDataExisting = IntPtr.Zero;
                    var logicalView = VSConstants.LOGVIEWID.Code_guid;
                    var openFlags = __VSOSEFLAGS.OSE_ChooseBestStdEditor;
                    var caption = "%2";  // %2 tells the IDE to call GetProperty(itemid, VSHPROPID_Caption, ...) to get the caption for the window.
                    Verify.HResult(uiShellOpenDocument.OpenStandardEditor(
                        (uint)openFlags,
                        filePath,
                        ref logicalView,
                        caption,
                        (IVsUIHierarchy)unconfiguredProject.Services.HostObject,
                        addedNode.GetHierarchyId(),
                        docDataExisting,
                        sp,
                        out var windowFrame));
                    //Guid projectDesignerGuid = new Guid("8a5aa6cf-46e3-4520-a70a-7393d15233e9");//_projectVsServices.VsHierarchy.GetGuidProperty(VsHierarchyPropID.ProjectDesignerEditor);

                 //   VsShellUtilities.OpenDocument(_serviceProvider, filePath, VSConstants.LOGVIEWID.Code_guid, out var hierarchy, out var itemId, out var windowFrame);
                }
            }

            return true;
        }
    }
}
