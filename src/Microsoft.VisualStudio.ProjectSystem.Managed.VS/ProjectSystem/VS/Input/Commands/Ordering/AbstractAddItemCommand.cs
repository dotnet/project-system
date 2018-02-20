// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

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

        protected abstract Task OnAddedNodesAsync(ConfiguredProject configuredProject, IEnumerable<IProjectTree> addedNodes, IProjectTree target);

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

            // Publish any existing changes that could potentially be here.
            // Then call OnAddingNodesAsync.
            // Then publish changes that could have taken place in OnAddingNodesAsync.
            await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);
            await OnAddingNodesAsync(nodeToAddTo).ConfigureAwait(true);
            await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);

            // Get the difference to see what was added.
            // We do a sanity check to make sure they have a valid display order.
            // We also order the added nodes by their display order.
            var updatedNode = _projectTree.CurrentTree.Find(node.Identity);
            var updatedNodeToAddTo = _projectTree.CurrentTree.Find(nodeToAddTo.Identity);
            var addedNodes =
                updatedNodeToAddTo.Children.Where(x => !nodeToAddTo.TryFind(x.Identity, out var subtree) && OrderingHelper.HasValidDisplayOrder(x))
                .OrderBy(OrderingHelper.GetDisplayOrder).ToList();

            if (addedNodes.Any())
            {
                await OnAddedNodesAsync(_projectVsServices.ActiveConfiguredProject, addedNodes, updatedNode).ConfigureAwait(true);
            }

            return true;
        }
    }
}
