// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(CommandGroup.ManagedProjectSystemOrder, ManagedProjectSystemOrderCommandId.AddExistingItemBelow)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class AddExistingItemBelowCommand : AbstractAddItemCommand
    {
        private readonly IAddItemDialogService _addItemDialogService;

        [ImportingConstructor]
        public AddExistingItemBelowCommand(
            IAddItemDialogService addItemDialogService,
            OrderAddItemHintReceiver orderAddItemHintReceiver) :
            base(addItemDialogService, orderAddItemHintReceiver)
        {
            _addItemDialogService = addItemDialogService;
        }

        protected override Task OnAddingNodesAsync(IProjectTree nodeToAddTo)
        {
            return _addItemDialogService.ShowAddExistingItemsDialogAsync(nodeToAddTo);
        }

        protected override bool CanAdd(IProjectTree target)
        {
            return OrderingHelper.HasValidDisplayOrder(target);
        }

        protected override OrderingMoveAction Action => OrderingMoveAction.MoveBelow;
    }
}
