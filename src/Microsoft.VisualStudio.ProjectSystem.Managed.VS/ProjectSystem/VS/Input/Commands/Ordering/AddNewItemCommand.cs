// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(CommandGroup.VisualStudioStandard97, (long)VSConstants.VSStd97CmdID.AddNewItem)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder + " & " + ProjectCapability.EditableDisplayOrder)]
    [Order(5000)]
    internal class AddNewItemCommand : AbstractAddItemCommand
    {
        private readonly IAddItemDialogService _addItemDialogService;

        [ImportingConstructor]
        public AddNewItemCommand(
            IAddItemDialogService addItemDialogService,
            OrderAddItemHintReceiver orderAddItemHintReceiver)
            : base(addItemDialogService, orderAddItemHintReceiver)
        {
            _addItemDialogService = addItemDialogService;
        }

        protected override Task OnAddingNodesAsync(IProjectTree nodeToAddTo)
        {
            return _addItemDialogService.ShowAddNewItemDialogAsync(nodeToAddTo);
        }

        protected override bool CanAdd(IProjectTree target)
        {
            return true;
        }
    }
}
