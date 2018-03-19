// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
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
        private readonly IProjectAccessor _accessor;

        public AbstractAddItemCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider, IProjectAccessor accessor)
        {
            Requires.NotNull(projectTree, nameof(IPhysicalProjectTree));
            Requires.NotNull(projectVsServices, nameof(IUnconfiguredProjectVsServices));
            Requires.NotNull(serviceProvider, nameof(SVsServiceProvider));

            _projectTree = projectTree;
            _projectVsServices = projectVsServices;
            _serviceProvider = serviceProvider;
            _accessor = accessor;
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

            var addedElements = await OrderingHelper.AddItems(_projectVsServices.ActiveConfiguredProject, _accessor, () => OnAddingNodesAsync(nodeToAddTo)).ConfigureAwait(false);

            if (addedElements.Any())
            {
                await _accessor.OpenProjectForWriteAsync(_projectVsServices.ActiveConfiguredProject, project => OnAddedElements(project, addedElements, node, nodeToAddTo)).ConfigureAwait(false);

                // Re-select the node that was the target.
                await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(false);
                await HACK_NodeHelper.SelectAsync(_projectVsServices.ActiveConfiguredProject, _serviceProvider, node).ConfigureAwait(false);

                // If the node we wanted to add to is a folder, make sure it is expanded.
                if (nodeToAddTo.IsFolder)
                {
                    await HACK_NodeHelper.ExpandFolderAsync(_projectVsServices.ActiveConfiguredProject, _serviceProvider, nodeToAddTo).ConfigureAwait(false);
                }
            }

            return true;
        }
    }
}
