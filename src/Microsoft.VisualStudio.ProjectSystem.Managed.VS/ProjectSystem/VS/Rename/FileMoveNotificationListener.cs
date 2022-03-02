// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Threading;
using Path = System.IO.Path;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [Order(Order.Default)]
    [Export(typeof(IFileMoveNotificationListener))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class FileMoveNotificationListener : IFileMoveNotificationListener
    {
        private const string _compileItemType = "Compile";
        private const string PromptNamespaceUpdate = "SolutionNavigator.PromptNamespaceUpdate";
        private const string EnableNamespaceUpdate = "SolutionNavigator.EnableNamespaceUpdate";

        private HashSet<Renamer.RenameDocumentActionSet>? _actions;
        private readonly SemaphoreSlim _semaphore = new(1,1);

        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsService<SVsOperationProgress, IVsOperationProgressStatusService> _operationProgressService;
        private readonly IWaitIndicator _waitService;
        private readonly IRoslynServices _roslynServices;
        private readonly IVsService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManagerService;

        [ImportingConstructor]
        public FileMoveNotificationListener(
            UnconfiguredProject unconfiguredProject,
            IUserNotificationServices userNotificationServices,
            IUnconfiguredProjectVsServices projectVsServices,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace,
            IProjectThreadingService threadingService,
            IVsService<SVsOperationProgress, IVsOperationProgressStatusService> operationProgressService,
            IWaitIndicator waitService,
            IRoslynServices roslynServices,
            IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService)

        {
            _unconfiguredProject = unconfiguredProject;
            _userNotificationServices = userNotificationServices;
            _projectVsServices = projectVsServices;
            _workspace = workspace;
            _threadingService = threadingService;
            _operationProgressService = operationProgressService;
            _waitService = waitService;
            _roslynServices = roslynServices;
            _settingsManagerService = settingsManagerService;
        }

        public async Task OnBeforeFilesMovedAsync(IReadOnlyCollection<IFileMoveItem> items)
        {
            _actions = new();
            Project? project = GetCurrentProject();
            if (project is null)
            {
                return;
            }

            if (!TryGetFilesToMove(items, out List<string>? filesToMove, out string destination))
            {
                return;
            }

            _actions = await GetNamespaceUpdateActionsAsync(project, filesToMove, destination);
        }

        public async Task OnAfterFileMoveAsync()
        {
            if (_actions?.Count == 0 || !await CheckUserConfirmationAsync())
            {
                return;
            }

            ApplyNamespaceUpdateActions(_actions!);
        }

        private static bool TryGetFilesToMove(IReadOnlyCollection<IFileMoveItem> items, [NotNullWhen(returnValue: true)] out List<string>? filesToMove, out string destination)
        {
            destination = string.Empty;
            filesToMove = null;

            // TODO : Parse children in Folders
            foreach (var item in items)
            {
                bool isCompileItem = item.ItemType is not null && item.ItemType.Equals(_compileItemType, System.StringComparison.OrdinalIgnoreCase);
                if (item.WithinProject && isCompileItem && !item.IsLinked && !item.IsFolder)
                {
                    filesToMove ??= new();
                    filesToMove.Add(item.Source);
                    destination = item.Destination;
                }
            }

            return filesToMove is not null;
        }

        private void ApplyNamespaceUpdateActions(HashSet<Renamer.RenameDocumentActionSet> actions)
        {
            foreach (var documentAction in actions)
            {
                _ = _threadingService.JoinableTaskFactory.RunAsync(async () =>
                {
                    await _semaphore.WaitAsync();

                    Solution currentSolution = await PublishLatestSolutionAsync(CancellationToken.None);

                    string actionMessage = documentAction.ApplicableActions.First().GetDescription(CultureInfo.CurrentCulture);

                    await _projectVsServices.ThreadingService.SwitchToUIThread();
                    WaitIndicatorResult<Solution> result = _waitService.Run(
                        title: VSResources.Renaming_Type,
                        message: actionMessage,
                        allowCancel: true,
                        token => documentAction.UpdateSolutionAsync(currentSolution, token));

                    // Do not warn the user if the rename was cancelled by the user
                    if (result.IsCancelled)
                    {
                        return;
                    }

                    _roslynServices.ApplyChangesToSolution(currentSolution.Workspace, result.Result);

                    _semaphore.Release();

                });
            }
        }

        private async Task<Solution> PublishLatestSolutionAsync(CancellationToken cancellationToken)
        {
            // WORKAROUND: We don't yet have a way to wait for the changes to propagate 
            // to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425), so 
            // instead we wait for the IntelliSense stage to finish for the entire solution

            var operationProgressStatusService = await _operationProgressService.GetValueAsync(cancellationToken);
            var stageStatus = operationProgressStatusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense);

            await stageStatus.WaitForCompletionAsync().WithCancellation(cancellationToken);

            // The result of that wait, is basically a "new" published Solution, so grab it
            return _workspace.CurrentSolution;
        }

        private async Task<bool> CheckUserConfirmationAsync()
        {
            ISettingsManager settings = await _settingsManagerService.GetValueAsync();
            bool promptNamespaceUpdate = settings.GetValueOrDefault(PromptNamespaceUpdate, true);
            bool enabledNamespaceUpdate = settings.GetValueOrDefault(EnableNamespaceUpdate, true);

            if (!enabledNamespaceUpdate || !promptNamespaceUpdate)
            {
                return enabledNamespaceUpdate;
            }

            await _projectVsServices.ThreadingService.SwitchToUIThread();

            bool confirmation = _userNotificationServices.Confirm(VSResources.UpdateNamespacePromptMessage, out promptNamespaceUpdate);

            await settings.SetValueAsync(PromptNamespaceUpdate, !promptNamespaceUpdate, true);

            return confirmation;
        }

        private async Task<HashSet<Renamer.RenameDocumentActionSet>> GetNamespaceUpdateActionsAsync(Project project, List<string> filesToMove, string destinationFilePath)
        {
            string destinationFileRelative = _unconfiguredProject.MakeRelative(destinationFilePath);
            string destinationFolder = Path.GetDirectoryName(destinationFileRelative);
            var documentFolders = destinationFolder.Split(new char[]{Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar}, System.StringSplitOptions.RemoveEmptyEntries);

            HashSet<Renamer.RenameDocumentActionSet> actions = new();

            foreach (var filenameWithPath in filesToMove)
            {
                string filename = Path.GetFileName(filenameWithPath);

                var oldDocument = project.Documents.FirstOrDefault(d => StringComparers.Paths.Equals(d.FilePath, filenameWithPath));
                if (oldDocument is null)
                {
                    continue;
                }

                // This is a file item to another directory, it should only detect this a Update Namespace action.
                // TODO: Upgrade this api to get rid of the exclamation sign
                Renamer.RenameDocumentActionSet documentAction = await Renamer.RenameDocumentAsync(oldDocument, null!, documentFolders);

                if (documentAction.ApplicableActions.IsEmpty ||
                    documentAction.ApplicableActions.Any(a => !a.GetErrors().IsEmpty))
                {
                    continue;
                }

                actions.Add(documentAction);
            }

            return actions;
        }

        private Project? GetCurrentProject() =>
            _workspace.CurrentSolution.Projects.FirstOrDefault(proj => StringComparers.Paths.Equals(proj.FilePath, _projectVsServices.Project.FullPath));
    }
}
