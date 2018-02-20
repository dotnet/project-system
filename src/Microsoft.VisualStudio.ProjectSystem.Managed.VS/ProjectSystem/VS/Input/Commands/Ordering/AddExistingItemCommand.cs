// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(CommandGroup.VisualStudioStandard97, (long)VSConstants.VSStd97CmdID.AddExistingItem)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    [Order(5000)]
    internal class AddExistingItemCommand : AbstractAddItemCommand
    {
        [ImportingConstructor]
        public AddExistingItemCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider) : base(projectTree, projectVsServices, serviceProvider)
        {
        }

        protected override IProjectTree GetNodeToAddTo(IProjectTree target)
        {
            return target;
        }

        protected override Task OnAddingNodesAsync(IProjectTree nodeToAddTo)
        {
            return ShowAddExistingFilesDialogAsync(nodeToAddTo);
        }

        protected override async Task OnAddedNodesAsync(ConfiguredProject configuredProject, IEnumerable<IProjectTree> addedNodes, IProjectTree target)
        {
            // Move added nodes to the top.
            var child = OrderingHelper.GetFirstChild(target);
            foreach (var addedNode in addedNodes)
            {
                if (child != addedNode)
                {
                    await OrderingHelper.TryMoveAboveAsync(configuredProject, addedNode, child).ConfigureAwait(true);
                }
            }
        }

        protected override bool CanAdd(IProjectTree target)
        {
            return true;
        }
    }
}
