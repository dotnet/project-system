using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [Export(typeof(IProjectTreeActionHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [Order(10)]
    internal class CheckForTypeToRename : ProjectTreeActionHandlerBase
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IEnvironmentOptions _environmentOptions;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IRenameTypeService _renameTypeService;

        [ImportingConstructor]
        public CheckForTypeToRename(IUnconfiguredProjectVsServices projectVsServices,
                                  IEnvironmentOptions environmentOptions,
                                  IUserNotificationServices userNotificationServices,
                                  IRenameTypeService renameTypeService)
        {
            _projectVsServices = projectVsServices;
            _environmentOptions = environmentOptions;
            _userNotificationServices = userNotificationServices;
            _renameTypeService = renameTypeService;
        }

        public override async Task RenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value)
        {
            if (!node.Flags.Contains(ProjectTreeFlags.SourceFile))
            {
                await base.RenameAsync(context, node, value);
                return;
            }

            string filePath = node.FilePath;

            // Check if there are any symbols that need to be renamed
            if (!await _renameTypeService.DoesFileNameMatchTypeAsync(filePath))
            {
                await base.RenameAsync(context, node, value);
                return;
            }

            string name = Path.GetFileNameWithoutExtension(filePath);

            // Ask if the user wants to rename the symbol
            bool userConfirmed = await CheckUserConfirmationAsync(name);
            if (!userConfirmed)
            {
                await base.RenameAsync(context, node, value);
                return;
            }

            // queue symbol to be renamed
            _renameTypeService.QueueFileWithTypeToBeRenamed(filePath);

            await base.RenameAsync(context, node, value);
        }

        private async Task<bool> CheckUserConfirmationAsync(string oldFileName)
        {
            await _projectVsServices.ThreadingService.SwitchToUIThread();
            bool userNeedPrompt = _environmentOptions.GetOption("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false);
            if (userNeedPrompt)
            {
                string renamePromptMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolPrompt, oldFileName);
                return _userNotificationServices.Confirm(renamePromptMessage);
            }

            return true;
        }
    }
}
