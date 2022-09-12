// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractAddItemCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IAddItemDialogService _addItemDialogService;
        private readonly OrderAddItemHintReceiver _orderAddItemHintReceiver;

        protected AbstractAddItemCommand(
            IAddItemDialogService addItemDialogService,
            OrderAddItemHintReceiver orderAddItemHintReceiver)
        {
            Requires.NotNull(addItemDialogService, nameof(addItemDialogService));
            Requires.NotNull(orderAddItemHintReceiver, nameof(orderAddItemHintReceiver));

            _addItemDialogService = addItemDialogService;
            _orderAddItemHintReceiver = orderAddItemHintReceiver;
        }

        protected abstract bool CanAdd(IProjectTree target);

        protected abstract Task OnAddingNodesAsync(IProjectTree nodeToAddTo);

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            IProjectTree? nodeToAddTo = GetNodeToAddTo(node);

            if (nodeToAddTo is not null && _addItemDialogService.CanAddNewOrExistingItemTo(nodeToAddTo) && CanAdd(node))
            {
                return GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled);
            }
            else
            {
                return GetCommandStatusResult.Unhandled;
            }
        }

        protected virtual OrderingMoveAction Action => OrderingMoveAction.MoveToTop;

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            IProjectTree? nodeToAddTo = GetNodeToAddTo(node);

            if (nodeToAddTo is null)
            {
                return false;
            }

            // We use a hint receiver that listens for when a file gets added.
            // The reason is so we can modify the MSBuild project inside the same write lock of when a file gets added internally in CPS.
            // This ensures that we only perform actions on the items that were added as result of a e.g. a add new/existing item dialog.
            await _orderAddItemHintReceiver.CaptureAsync(Action, node, () => OnAddingNodesAsync(nodeToAddTo));

            return true;
        }

        private IProjectTree? GetNodeToAddTo(IProjectTree node)
        {
            return Action switch
            {
                OrderingMoveAction.MoveAbove or OrderingMoveAction.MoveBelow => node.Parent,
                _ => node,
            };
        }
    }
}
