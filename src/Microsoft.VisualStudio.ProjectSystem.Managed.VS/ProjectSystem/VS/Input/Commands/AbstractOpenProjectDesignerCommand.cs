// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractOpenProjectDesignerCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IProjectDesignerService _designerService;

        protected AbstractOpenProjectDesignerCommand(IProjectDesignerService designerService)
        {
            Requires.NotNull(designerService, nameof(designerService));

            _designerService = designerService;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            // We assume that if the AppDesignerTreeModifier marked an AppDesignerFolder, that we must support the Project Designer
            if (node.Flags.Contains(ProjectTreeFlags.Common.AppDesignerFolder))
            {
                return GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled);
            }

            return GetCommandStatusResult.Unhandled;
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (node.Flags.Contains(ProjectTreeFlags.Common.AppDesignerFolder))
            {
                await _designerService.ShowProjectDesignerAsync();
                return true;
            }

            return false;
        }
    }
}
