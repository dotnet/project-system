// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(CommandGroup.VisualStudioStandard97, VisualStudioStandard97CommandId.AddClass)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class AddClassProjectVBCommand : AbstractAddClassProjectCommand
    {
        [ImportingConstructor]
        public AddClassProjectVBCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<SVsAddProjectItemDlg, IVsAddProjectItemDlg> addItemDialog) 
            : base(projectTree, projectVsServices, addItemDialog)
        {
        }

        protected override string DirName { get; } = "Common Items";
    }
}
