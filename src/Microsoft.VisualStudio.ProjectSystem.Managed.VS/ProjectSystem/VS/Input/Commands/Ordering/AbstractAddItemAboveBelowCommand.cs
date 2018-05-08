// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractAddItemAboveBelowCommand : AbstractAddItemCommand
    {
        [ImportingConstructor]
        public AbstractAddItemAboveBelowCommand(
            IPhysicalProjectTree projectTree,
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            OrderAddItemHintReceiver orderAddItemHintReceiver,
            ConfiguredProject configuredProject,
            IProjectAccessor accessor) :
            base(projectTree, projectVsServices, serviceProvider, orderAddItemHintReceiver, configuredProject, accessor)
        {
        }

        protected override bool CanAdd(Project project, IProjectTree target)
        {
            // Check to make sure the target has valid backing xml elements that are part of the project, if it does we can move.
            return OrderingHelper.HasValidDisplayOrder(target) && OrderingHelper.GetItemElements(project, target, ImmutableArray<string>.Empty).Any();
        }
    }
}
