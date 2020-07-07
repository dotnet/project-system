// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [Order(Order.Default)]
    [Export(typeof(IProjectTreeActionHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal partial class RenamerProjectTreeActionHandler : ProjectTreeActionHandlerBase
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
        private readonly IVsService<SVsOperationProgress, IVsOperationProgressStatusService> _operationProgressService;

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
            IVsUIService<IVsExtensibility, IVsExtensibility3> extensibility,
            IVsService<SVsOperationProgress, IVsOperationProgressStatusService> operationProgressService)
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
            _operationProgressService = operationProgressService;
        }

        protected virtual async Task CPSRenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value)
        {
            await base.RenameAsync(context, node, value);
        }

        public override async Task RenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value)
        {
            Requires.NotNull(context, nameof(Context));
            Requires.NotNull(node, nameof(node));
            Requires.NotNullOrEmpty(value, nameof(value));

            string? oldFilePath = node.FilePath;
            string oldName = Path.GetFileNameWithoutExtension(oldFilePath);
            string newFileWithExtension = value;
            CodeAnalysis.Project? project = GetCurrentProject();

            // Rename the file
            await CPSRenameAsync(context, node, value);

            if (await IsAutomationFunctionAsync() || node.IsFolder || _vsOnlineServices.ConnectedToVSOnline)
            {
                // Do not display rename Prompt
                return;
            }

            if (project is null)
            {
                return;
            }

            string newName = Path.GetFileNameWithoutExtension(newFileWithExtension);
            if (!await CanRenameType(project, oldName, newName))
            {
                return;
            }

            (bool result, Renamer.RenameDocumentActionSet? documentRenameResult) = await GetRenameSymbolsActions(project, oldFilePath, newFileWithExtension);
            if (result == false || documentRenameResult == null)
            {
                return;
            }

            // Ask if the user wants to rename the symbol
            bool userWantsToRenameSymbol = await CheckUserConfirmation(oldName);
            if (!userWantsToRenameSymbol)
                return;

            _threadingService.RunAndForget(async () =>
            {
                // TODO - implement PublishAsync() to sync with LanguageService
                // https://github.com/dotnet/project-system/issues/3425)
                // await _languageService.PublishAsync(treeVersion);
                IVsOperationProgressStageStatus stageStatus = (await _operationProgressService.GetValueAsync()).GetStageStatus(CommonOperationProgressStageIds.Intellisense);
                await stageStatus.WaitForCompletionAsync();

                // Apply actions and notify other VS features
                CodeAnalysis.Solution? currentSolution = GetCurrentProject()?.Solution;
                if (currentSolution == null)
                {
                    return;
                }
                string renameOperationName = string.Format(CultureInfo.CurrentCulture, VSResources.Renaming_Type_from_0_to_1, oldName, value);
                (WaitIndicatorResult result, CodeAnalysis.Solution renamedSolution) = _waitService.WaitForAsyncFunctionWithResult(
                                title: VSResources.Renaming_Type,
                                message: renameOperationName,
                                allowCancel: true,
                                token => documentRenameResult.UpdateSolutionAsync(currentSolution, token));

                // Do not warn the user if the rename was cancelled by the user	
                if (result.WasCanceled())
                {
                    return;
                }

                await _projectVsServices.ThreadingService.SwitchToUIThread();
                if (!_roslynServices.ApplyChangesToSolution(currentSolution.Workspace, renamedSolution))
                {
                    string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldName);
                    _userNotificationServices.ShowWarning(failureMessage);
                }
                return;
            }, _unconfiguredProject);
        }

        private static async Task<(bool, Renamer.RenameDocumentActionSet?)> GetRenameSymbolsActions(CodeAnalysis.Project project, string? oldFilePath, string newFileWithExtension)
        {
            CodeAnalysis.Document? oldDocument = GetDocument(project, oldFilePath);
            if (oldDocument is null)
            {
                return (false, null);
            }

            // Get the list of possible actions to execute
            Renamer.RenameDocumentActionSet documentRenameResult = await Renamer.RenameDocumentAsync(oldDocument, newFileWithExtension);

            // Check if there are any symbols that need to be renamed
            if (documentRenameResult.ApplicableActions.IsEmpty)
            {
                return (false, documentRenameResult);
            }

            // Check errors before applying changes
            if (documentRenameResult.ApplicableActions.Any(a => a.GetErrors().Length > 0))
                return (false, documentRenameResult);

            return (true, documentRenameResult);
        }

        private async Task<bool> CanRenameType(CodeAnalysis.Project? project, string oldName, string newName)
        {
            // see if the current project contains a compilation
            (bool success, bool isCaseSensitive) = await TryDetermineIfCompilationIsCaseSensitiveAsync(project);
            if (!success)
            {
                return false;
            }

            if (!CanHandleRename(oldName, newName, isCaseSensitive))
            {
                return false;
            }
            return true;
        }

        private bool CanHandleRename(string oldName, string newName, bool isCaseSensitive)
            => _roslynServices.IsValidIdentifier(oldName) &&
               _roslynServices.IsValidIdentifier(newName) &&
              (!string.Equals(
                  oldName,
                  newName,
                  isCaseSensitive
                    ? StringComparisons.LanguageIdentifiers
                    : StringComparisons.LanguageIdentifiersIgnoreCase));

        private static async Task<(bool success, bool isCaseSensitive)> TryDetermineIfCompilationIsCaseSensitiveAsync(CodeAnalysis.Project? project)
        {
            if (project is null)
                return (false, false);

            Compilation? compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                // this project does not support compilations
                return (false, false);
            }

            return (true, compilation.IsCaseSensitive);
        }

        protected virtual async Task<bool> IsAutomationFunctionAsync()
        {
            await _threadingService.SwitchToUIThread();

            _extensibility.Value.IsInAutomationFunction(out int isInAutomationFunction);
            return isInAutomationFunction != 0;
        }

        private CodeAnalysis.Project? GetCurrentProject() =>
            _workspace.CurrentSolution.Projects.FirstOrDefault(proj => StringComparers.Paths.Equals(proj.FilePath, _projectVsServices.Project.FullPath));

        private static CodeAnalysis.Document GetDocument(CodeAnalysis.Project project, string? filePath) =>
            project.Documents.FirstOrDefault(d => StringComparers.Paths.Equals(d.FilePath, filePath));

        private async Task<bool> CheckUserConfirmation(string oldFileName)
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
