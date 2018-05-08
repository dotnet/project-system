// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
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
        private readonly OrderAddItemHintReceiver _orderAddItemHintReceiver;
        private readonly ConfiguredProject _configuredProject;
        private readonly IProjectAccessor _accessor;

        public AbstractAddItemCommand(
            IPhysicalProjectTree projectTree, 
            IUnconfiguredProjectVsServices projectVsServices, 
            SVsServiceProvider serviceProvider,
            OrderAddItemHintReceiver orderAddItemHintReceiver,
            ConfiguredProject configuredProject, 
            IProjectAccessor accessor)
        {
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(orderAddItemHintReceiver, nameof(orderAddItemHintReceiver));
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(accessor, nameof(_accessor));

            _projectTree = projectTree;
            _projectVsServices = projectVsServices;
            _serviceProvider = serviceProvider;
            _orderAddItemHintReceiver = orderAddItemHintReceiver;
            _configuredProject = configuredProject;
            _accessor = accessor;
        }

        protected abstract bool CanAdd(Project project, IProjectTree target);

        protected abstract Task OnAddingNodesAsync(IProjectTree nodeToAddTo);

        protected Task ShowAddNewFileDialogAsync(IProjectTree target)
        {
            return HACK_AddItemHelper.ShowAddNewFileDialogAsync(_projectTree, _projectVsServices, _serviceProvider, target);
        }

        protected Task ShowAddExistingFilesDialogAsync(IProjectTree target)
        {
            return HACK_AddItemHelper.ShowAddExistingFilesDialogAsync(_projectTree, _projectVsServices, _serviceProvider, target);
        }

        protected override async Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            var canAdd = false;

            await _accessor.OpenProjectForWriteAsync(_configuredProject, project => canAdd = CanAdd(project, node)).ConfigureAwait(false);

            if (_projectTree.NodeCanHaveAdditions(GetNodeToAddTo(node)) && canAdd)
            {
                return await GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled).ConfigureAwait(false);
            }
            else
            {
                return await GetCommandStatusResult.Unhandled.ConfigureAwait(false);
            }
        }

        protected virtual OrderingMoveAction Action => OrderingMoveAction.MoveToTop;

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            var nodeToAddTo = GetNodeToAddTo(node);

            // We use a hint receiver that listens for when a file gets added.
            // The reason is so we can modify the MSBuild project inside the same write lock of when a file gets added internally in CPS.
            // This ensures that we only perform actions on the items that were added as result of a e.g. a add new/existing item dialog.
            await _orderAddItemHintReceiver.Capture(Action, node, () => OnAddingNodesAsync(nodeToAddTo)).ConfigureAwait(false);

            return true;
        }

        private IProjectTree GetNodeToAddTo(IProjectTree node)
        {
            IProjectTree target;
            switch (Action)
            {
                case OrderingMoveAction.MoveAbove:
                case OrderingMoveAction.MoveBelow:
                    target = node.Parent;
                    break;
                default:
                    target = node;
                    break;
            }

            return target;
        }
    }
}
