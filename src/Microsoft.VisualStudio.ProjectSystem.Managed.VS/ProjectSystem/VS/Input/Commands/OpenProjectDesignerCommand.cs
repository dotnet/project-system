// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    // Opens the Project Designer ("Property Pages") when selecting the Open menu item on the AppDesigner folder
    [ProjectCommand(CommandGroup.VisualStudioStandard97, VisualStudioStandard97CommandId.Open)]
    [AppliesTo(ProjectCapability.AppDesigner)]
    [Order(Order.Default)]
    internal class OpenProjectDesignerCommand : AbstractOpenProjectDesignerCommand
    {
        [ImportingConstructor]
        public OpenProjectDesignerCommand(IProjectDesignerService designerService)
            : base(designerService)
        {
        }
    }
}
