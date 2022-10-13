// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Runtime.Remoting.Contexts;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Solution = Microsoft.CodeAnalysis.Solution;

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
        private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IVsUIService<IVsExtensibility, IVsExtensibility3> _extensibility;
        private readonly IVsOnlineServices _vsOnlineServices;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IWaitIndicator _waitService;
        private readonly IRoslynServices _roslynServices;
        private readonly Workspace _workspace;
        private readonly IVsService<SVsOperationProgress, IVsOperationProgressStatusService> _operationProgressService;
        private readonly IVsService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManagerService;

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
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService projectAsynchronousTasksService,
            IProjectThreadingService threadingService,
            IVsUIService<IVsExtensibility, IVsExtensibility3> extensibility,
            IVsService<SVsOperationProgress, IVsOperationProgressStatusService> operationProgressService,
            IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService)
        {
            _unconfiguredProject = unconfiguredProject;
            _projectVsServices = projectVsServices;
            _workspace = workspace;
            _environmentOptions = environmentOptions;
            _userNotificationServices = userNotificationServices;
            _roslynServices = roslynServices;
            _waitService = waitService;
            _vsOnlineServices = vsOnlineServices;
            _projectAsynchronousTasksService = projectAsynchronousTasksService;
            _threadingService = threadingService;
            _extensibility = extensibility;
            _operationProgressService = operationProgressService;
            _settingsManagerService = settingsManagerService;
        }

        protected virtual Task CpsFileRenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value)
        {
            return base.RenameAsync(context, node, value);
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

            await CpsFileRenameAsync(context, node, value);

            if (project is null ||
                await IsAutomationFunctionAsync() ||
                node.IsFolder ||
                _vsOnlineServices.ConnectedToVSOnline ||
                FileChangedExtension(oldFilePath, newFileWithExtension))
            {
                // Do not display rename Prompt
                return;
            }

            string newName = Path.GetFileNameWithoutExtension(newFileWithExtension);
            if (!await CanRenameTypeAsync(project, oldName, newName))
            {
                return;
            }

            (bool result, Renamer.RenameDocumentActionSet? documentRenameResult) = await GetRenameSymbolsActionsAsync(project, oldFilePath, newFileWithExtension);
            if (!result || documentRenameResult is null)
            {
                return;
            }

            // Ask if the user wants to rename the symbol
            bool userWantsToRenameSymbol = await CheckUserConfirmationAsync(oldName);
            if (!userWantsToRenameSymbol)
            {
                return;
            }

            _threadingService.RunAndForget(async () =>
            {
                Solution currentSolution = await PublishLatestSolutionAsync(_projectAsynchronousTasksService.UnloadCancellationToken);

                string renameOperationName = string.Format(CultureInfo.CurrentCulture, VSResources.Renaming_Type_from_0_to_1, oldName, value);
                WaitIndicatorResult<Solution> indicatorResult = _waitService.Run(
                                title: VSResources.Renaming_Type,
                                message: renameOperationName,
                                allowCancel: true,
                                context => documentRenameResult.UpdateSolutionAsync(currentSolution, context.CancellationToken));

                // Do not warn the user if the rename was cancelled by the user	
                if (indicatorResult.IsCancelled)
                {
                    return;
                }

                await _projectVsServices.ThreadingService.SwitchToUIThread();
                if (_roslynServices.ApplyChangesToSolution(currentSolution.Workspace, indicatorResult.Result))
                {
                    return;
                }

                string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldName);
                _userNotificationServices.ShowWarning(failureMessage);
            }, _unconfiguredProject);
        }

        private static bool FileChangedExtension(string? oldFilePath, string newFileWithExtension)
            => !StringComparers.Paths.Equals(Path.GetExtension(oldFilePath), Path.GetExtension(newFileWithExtension));

        private async Task<Solution> PublishLatestSolutionAsync(CancellationToken cancellationToken)
        {
            // WORKAROUND: We don't yet have a way to wait for the rename changes to propagate 
            // to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425), so 
            // instead we wait for the IntelliSense stage to finish for the entire solution
            IVsOperationProgressStatusService operationProgressStatusService = await _operationProgressService.GetValueAsync(cancellationToken);
            IVsOperationProgressStageStatus stageStatus = operationProgressStatusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense);

            await stageStatus.WaitForCompletionAsync().WithCancellation(cancellationToken);

            // The result of that wait, is basically a "new" published Solution, so grab it
            return _workspace.CurrentSolution;
        }

        private static async Task<(bool, Renamer.RenameDocumentActionSet?)> GetRenameSymbolsActionsAsync(CodeAnalysis.Project project, string? oldFilePath, string newFileWithExtension)
        {
            CodeAnalysis.Document? oldDocument = GetDocument(project, oldFilePath);
            if (oldDocument is null)
            {
                return (false, null);
            }

            // Get the list of possible actions to execute
#pragma warning disable CS0618 // Type or member is obsolete https://github.com/dotnet/project-system/issues/8591
            Renamer.RenameDocumentActionSet documentRenameResult = await Renamer.RenameDocumentAsync(oldDocument, newFileWithExtension);
#pragma warning restore CS0618 // Type or member is obsolete

            // Check if there are any symbols that need to be renamed
            if (documentRenameResult.ApplicableActions.IsEmpty)
            {
                return (false, documentRenameResult);
            }

            // Check errors before applying changes
            if (documentRenameResult.ApplicableActions.Any(a => !a.GetErrors().IsEmpty))
            {
                return (false, documentRenameResult);
            }

            return (true, documentRenameResult);
        }

        private async Task<bool> CanRenameTypeAsync(CodeAnalysis.Project? project, string oldName, string newName)
        {
            // see if the current project contains a compilation
            (bool success, bool isCaseSensitive) = await TryDetermineIfCompilationIsCaseSensitiveAsync(project);

            return success && CanHandleRename(oldName, newName, isCaseSensitive);
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
            return compilation is null ? (false, false) : (true, compilation.IsCaseSensitive);
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

        private async Task<bool> CheckUserConfirmationAsync(string oldFileName)
        {
            ISettingsManager settings = await _settingsManagerService.GetValueAsync();

            // Default value needs to match the default value in the checkbox Tools|Options|Project and Solutions|Enable symbolic renaming.
            bool enableSymbolicRename = settings.GetValueOrDefault(VsToolsOptions.OptionEnableSymbolicRename, true);

            await _projectVsServices.ThreadingService.SwitchToUIThread();

            bool userNeedPrompt = _environmentOptions.GetOption(VsToolsOptions.CategoryEnvironment, VsToolsOptions.PageProjectsAndSolution, VsToolsOptions.OptionPromptRenameSymbol, false);

            if (!enableSymbolicRename || !userNeedPrompt)
            {
                return enableSymbolicRename;
            }

            string renamePromptMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolPrompt, oldFileName);

            bool shouldRename = _userNotificationServices.Confirm(renamePromptMessage, out bool disablePromptMessage);

            if (disablePromptMessage)
            {
                await settings.SetValueAsync(VsToolsOptions.OptionEnableSymbolicRename, shouldRename, isMachineLocal: true);
                _environmentOptions.SetOption(VsToolsOptions.CategoryEnvironment, VsToolsOptions.PageProjectsAndSolution, VsToolsOptions.OptionPromptRenameSymbol, !disablePromptMessage);
            }

            return shouldRename;
        }
    }
}
