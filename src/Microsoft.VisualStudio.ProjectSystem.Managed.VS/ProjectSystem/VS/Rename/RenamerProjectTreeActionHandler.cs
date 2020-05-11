// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Rename;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [Order(Order.Default)]
    [Export(typeof(IProjectTreeActionHandler))]
    [Export(typeof(IFileRenameHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class RenamerProjectTreeActionHandler : ProjectTreeActionHandlerBase, IFileRenameHandler
    {
        private readonly IEnvironmentOptions _environmentOptions;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IProjectThreadingService _threadingService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IVsUIService<IVsExtensibility, IVsExtensibility3> _extensibility;
        private readonly IVsOnlineServices _vsOnlineServices;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IWaitIndicator _waitService;
        private readonly IRoslynServices _roslynServices;
        private readonly Workspace _workspace;
        private bool _isLanguageServiceDone;
        private string _oldFilePath;

        [ImportingConstructor]
        public RenamerProjectTreeActionHandler(
            UnconfiguredProject unconfiguredProject,
            IUnconfiguredProjectVsServices projectVsServices,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace,
            IEnvironmentOptions environmentOptions,
            IUserNotificationServices userNotificationServices,
            IRoslynServices roslynServices,
            IWaitIndicator waitService,
            IVsOnlineServices vsOnlineServices,
            IProjectThreadingService threadingService,
            IVsUIService<IVsExtensibility, IVsExtensibility3> extensibility)
        {
            _unconfiguredProject = unconfiguredProject;
            _projectVsServices = projectVsServices;
            _workspace = workspace;
            _environmentOptions = environmentOptions;
            _userNotificationServices = userNotificationServices;
            _roslynServices = roslynServices;
            _waitService = waitService;
            _vsOnlineServices = vsOnlineServices;
            _threadingService = threadingService;
            _extensibility = extensibility;
        }

        public void HandleRename(string oldFilePath, string newFilePath)
        {
            if (_oldFilePath == oldFilePath)
            {
                _isLanguageServiceDone = true;
            }
        }

        public override async Task RenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value)
        {
            Assumes.Present(context);
            Assumes.Present(node);
            Assumes.Present(value);

            _oldFilePath = node.FilePath;
            string? newNameWithExtension = value;
            // These variables are need to synchronize with Roslyn
            _isLanguageServiceDone = false;

            // Do not offer to rename the file in VS Online scenarios.
            if (_vsOnlineServices.ConnectedToVSOnline)
            {
                return;
            }

            if (await IsAutomationFunction() || node.IsFolder)
            {
                // Do not display rename Prompt
                await base.RenameAsync(context, node, value);
                return;
            }

            // Get the list of possible actions to execute
            string oldName = Path.GetFileNameWithoutExtension(_oldFilePath);
            CodeAnalysis.Project? project = GetCurrentProject();
            if (project is null)
            {
                return;
            }

            CodeAnalysis.Document? oldDocument = GetDocument(project, _oldFilePath);
            if (oldDocument is null)
            {
                return;
            }

            var documentRenameResult = await CodeAnalysis.Rename.Renamer.RenameDocumentAsync(oldDocument, newNameWithExtension, oldDocument.Folders);

            bool errorsDetected = false;
            foreach (var action in documentRenameResult.ApplicableActions)
            {
                foreach (var error in action.GetErrors())
                {
                    errorsDetected = true;
                }
            }

            // Check errors before applying changes
            if (errorsDetected)
            {
                string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldName);
                _userNotificationServices.ShowWarning(failureMessage);
                return;
            }

            // Rename the file
            await base.RenameAsync(context, node, value); // Async process (1 of which updating Roslyn)

            //Check if there are any symbols that need to be renamed
            if (documentRenameResult.ApplicableActions.IsEmpty)
                return;

            // Ask if the user wants to rename the symbol
            bool userWantsToRenameSymbol = await CheckUserConfirmation(oldName);
            if (!userWantsToRenameSymbol)
                return;

            _threadingService.RunAndForget(async () =>
            {
                // TODO - implement PublishAsync() to sync with LanguageService
                // https://github.com/dotnet/project-system/issues/3425)
                // await _languageService.PublishAsync(treeVersion);
                //
                // Because HandleRename() is called after Roslyn, we can set this 
                // flag when node.FilePath == HandlRename(FilePath)
                while (_isLanguageServiceDone == false) ;

                // Apply actions and notify other VS features
                CodeAnalysis.Solution currentSolution = GetCurrentProject().Solution;
                string renameOperationName = string.Format(CultureInfo.CurrentCulture, VSResources.Renaming_Type_from_0_to_1, oldName, value);
                (WaitIndicatorResult result, CodeAnalysis.Solution renamedSolution) = _waitService.WaitForAsyncFunctionWithResult(
                                title: VSResources.Renaming_Type,
                                message: renameOperationName,
                                allowCancel: true,
                                token => documentRenameResult.UpdateSolutionAsync(currentSolution, token));

                _roslynServices.ApplyChangesToSolution(currentSolution.Workspace, renamedSolution);
                return;

            }, _unconfiguredProject);

        }

        private async Task<bool> IsAutomationFunction()
        {
            await _threadingService.SwitchToUIThread();

            _extensibility.Value.IsInAutomationFunction(out int isInAutomationFunction);
            return isInAutomationFunction != 0;
        }

        private CodeAnalysis.Project? GetCurrentProject()
        {
            foreach (CodeAnalysis.Project proj in _workspace.CurrentSolution.Projects)
            {
                if (StringComparers.Paths.Equals(proj.FilePath, _projectVsServices.Project.FullPath))
                {
                    return proj;
                }
            }

            return null;
        }

        private static CodeAnalysis.Document GetDocument(CodeAnalysis.Project? project, string? filePath) =>
            (from d in project?.Documents where StringComparers.Paths.Equals(d.FilePath, filePath) select d).FirstOrDefault();

        private async Task<bool> CheckUserConfirmation(string oldFileName)
        {
            await _projectVsServices.ThreadingService.SwitchToUIThread();
            bool userNeedPrompt = _environmentOptions.GetOption("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false);
            if (userNeedPrompt)
            {
                string renamePromptMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolPrompt, oldFileName);

                await _projectVsServices.ThreadingService.SwitchToUIThread();
                return _userNotificationServices.Confirm(renamePromptMessage);
            }

            return true;
        }
    }
}
