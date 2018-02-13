// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemOrderCommandSet, ManagedProjectSystemPackage.MoveUpCmdId)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class MoveUpCommand : AbstractMoveCommand
    {
        [ImportingConstructor]
        public MoveUpCommand(IPhysicalProjectTree projectTree, SVsServiceProvider serviceProvider, ConfiguredProject configuredProject) : base(projectTree, serviceProvider, configuredProject)
        {
        }

        protected override bool CanMove(IProjectTree node)
        {
            return OrderingHelper.CanMoveUp(node);
        }

        protected override Task<bool> TryMoveAsync(ConfiguredProject configuredProject, IProjectTree node)
        {
            return OrderingHelper.TryMoveUpAsync(configuredProject, node);
        }
    }
}
