// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemOrderCommandSet, ManagedProjectSystemPackage.AddNewItemAboveCmdId)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class AddNewItemAboveCommand : AbstractAddItemCommand
    {
        [ImportingConstructor]
        public AddNewItemAboveCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider, IProjectAccessor accessor) : base(projectTree, projectVsServices, serviceProvider, accessor)
        {
        }

        protected override IProjectTree GetNodeToAddTo(IProjectTree target)
        {
            return target.Parent;
        }

        protected override Task OnAddingNodesAsync(IProjectTree nodeToAddTo)
        {
            return ShowAddNewFileDialogAsync(nodeToAddTo);
        }

        protected override void OnAddedElements(Project project, ImmutableArray<ProjectItemElement> elements, IProjectTree target, IProjectTree nodeToAddTo)
        {
            OrderingHelper.TryMoveElementsAbove(project, elements, target);
        }

        protected override bool CanAdd(IProjectTree target)
        {
            return OrderingHelper.HasValidDisplayOrder(target);
        }
    }
}
