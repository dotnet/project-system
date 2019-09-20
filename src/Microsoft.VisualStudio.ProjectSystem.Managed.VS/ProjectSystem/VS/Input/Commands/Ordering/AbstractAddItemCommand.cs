// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Task = System.Threading.Tasks.Task;

#nullable disable

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

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (_addItemDialogService.CanAddNewOrExistingItemTo(GetNodeToAddTo(node)) && CanAdd(node))
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
            IProjectTree nodeToAddTo = GetNodeToAddTo(node);

            // We use a hint receiver that listens for when a file gets added.
            // The reason is so we can modify the MSBuild project inside the same write lock of when a file gets added internally in CPS.
            // This ensures that we only perform actions on the items that were added as result of a e.g. a add new/existing item dialog.
            await _orderAddItemHintReceiver.Capture(Action, node, () => OnAddingNodesAsync(nodeToAddTo));

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
