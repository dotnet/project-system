// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemOrderCommandSet, ManagedProjectSystemPackage.AddExistingItemAboveCmdId)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class AddExistingItemAboveCommand : AbstractAddItemAboveBelowCommand
    {
        [ImportingConstructor]
        public AddExistingItemAboveCommand(
            IPhysicalProjectTree projectTree,
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            OrderAddItemHintReceiver orderAddItemHintReceiver,
            ConfiguredProject configuredProject,
            IProjectAccessor accessor) :
            base(projectTree, projectVsServices, serviceProvider, orderAddItemHintReceiver, configuredProject, accessor)
        {
        }

        protected override Task OnAddingNodesAsync(IProjectTree nodeToAddTo)
        {
            return ShowAddExistingFilesDialogAsync(nodeToAddTo);
        }

        protected override OrderingMoveAction Action => OrderingMoveAction.MoveAbove;
    }
}
