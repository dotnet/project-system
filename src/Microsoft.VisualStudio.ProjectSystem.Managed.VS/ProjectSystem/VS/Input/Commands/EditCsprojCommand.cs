using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(ManagedProjectSystemPackage.DotnetProjectSystemCommandSet, ManagedProjectSystemPackage.EditCsprojCmdid)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class EditCsprojCommand : AbstractSingleNodeProjectCommand
    {
        readonly IUnconfiguredProjectVsServices _projectVsServices;

        [ImportingConstructor]
        public EditCsprojCommand(IUnconfiguredProjectVsServices projectVsServices)
        {
            _projectVsServices = projectVsServices;
        }

        protected override async Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, Boolean focused, String commandText, CommandStatus progressiveStatus)
        {
            if (node.IsRoot())
            {
                var projProperties = await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
                return await GetCommandStatusResult.Unhandled.ConfigureAwait(false);
                
            }
            else
            {
                return await GetCommandStatusResult.Unhandled.ConfigureAwait(false);
            }

        }

        protected override Task<Boolean> TryHandleCommandAsync(IProjectTree node, Boolean focused, Int64 commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            return Task.FromResult(true);
        }
    }
}
