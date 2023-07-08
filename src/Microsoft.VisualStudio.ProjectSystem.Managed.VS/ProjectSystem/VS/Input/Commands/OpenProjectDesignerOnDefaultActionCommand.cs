// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    // Opens the Project Designer ("Property Pages") when the user double-clicks or presses ENTER on the AppDesigner folder while its selected
    [ProjectCommand(CommandGroup.UIHierarchyWindow, UIHierarchyWindowCommandId.DoubleClick, UIHierarchyWindowCommandId.EnterKey)]
    [AppliesTo(ProjectCapability.AppDesigner)]
    [Order(Order.Default)]
    internal class OpenProjectDesignerOnDefaultActionCommand : AbstractOpenProjectDesignerCommand
    {
        [ImportingConstructor]
        public OpenProjectDesignerOnDefaultActionCommand(IProjectDesignerService designerService)
            : base(designerService)
        {
        }
    }
}
