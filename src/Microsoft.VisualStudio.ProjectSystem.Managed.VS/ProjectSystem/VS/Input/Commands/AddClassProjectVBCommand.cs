// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Input;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(CommandGroup.VisualStudioStandard97, VisualStudioStandard97CommandId.AddClass)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    class AddClassProjectVBCommand : AbstractAddClassProjectCommand
    {
        [ImportingConstructor]
        public AddClassProjectVBCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider) : base(projectTree, projectVsServices, serviceProvider)
        {
        }

        protected override string DirName { get; } = "Common Items";
    }
}
