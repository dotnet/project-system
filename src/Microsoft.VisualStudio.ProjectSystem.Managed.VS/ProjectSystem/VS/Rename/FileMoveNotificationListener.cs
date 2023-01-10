// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
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
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsService<SVsOperationProgress, IVsOperationProgressStatusService> _operationProgressService;
        private readonly IWaitIndicator _waitService;
        private readonly IRoslynServices _roslynServices;
        private readonly IVsService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManagerService;

        private List<(Renamer.RenameDocumentActionSet Set, string FileName)>? _actions;

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
            Project? project = GetCurrentProject();

            if (project is not null && TryGetFilesToMove(out List<(string file, string destination)>? filesToMove))
            {
                _actions = await GetNamespaceUpdateActionsAsync();
            }
            else
            {
                _actions = null;
            }

            return;

            Project? GetCurrentProject()
            {
                return _workspace.CurrentSolution.Projects.FirstOrDefault(
                    proj => StringComparers.Paths.Equals(proj.FilePath, _projectVsServices.Project.FullPath));
            }

            bool TryGetFilesToMove([NotNullWhen(returnValue: true)] out List<(string file, string destination)>? filesToMove)
            {
                filesToMove = null;

                foreach (IFileMoveItem item in items)
                {
                    RecursiveTryGetFilesToMove(item, ref filesToMove);
                }

                return filesToMove is not null;
            }

            void RecursiveTryGetFilesToMove(IFileMoveItem? item, ref List<(string file, string destination)>? filesToMove)
            {
                if (item is null)
                {
                    return;
                }

                if (item.IsFolder)
                {
                    if (item is not ICopyPasteItem copyPasteItem)
                    {
                        return;
                    }

                    foreach (IFileMoveItem child in copyPasteItem.Children.Cast<IFileMoveItem>())
                    {
                        RecursiveTryGetFilesToMove(child, ref filesToMove);
                    }
                }
                else
                {
                    bool isCompileItem = StringComparers.ItemTypes.Equals(item.ItemType, Compile.SchemaName);

                    if (item.WithinProject && isCompileItem && !item.IsLinked && !item.IsFolder)
                    {
                        filesToMove ??= new();
                        filesToMove.Add((item.Source, item.Destination));
                    }
                }
            }

            async Task<List<(Renamer.RenameDocumentActionSet, string)>> GetNamespaceUpdateActionsAsync()
            {
                List<(Renamer.RenameDocumentActionSet, string)> actions = new();

                foreach ((string filenameWithPath, string destination) in filesToMove)
                {
                    string destinationFileRelative = _unconfiguredProject.MakeRelative(destination);
                    string destinationFolder = Path.GetDirectoryName(destinationFileRelative);
                    string[] documentFolders = destinationFolder.Split(Delimiter.Path, StringSplitOptions.RemoveEmptyEntries);

                    string filename = Path.GetFileName(filenameWithPath);

                    Document? oldDocument = project.Documents.FirstOrDefault(d => StringComparers.Paths.Equals(d.FilePath, filenameWithPath));

                    if (oldDocument is null)
                    {
                        continue;
                    }

                    // This is a file item to another directory, it should only detect this a Update Namespace action.
                    // TODO Upgrade this api to get rid of the exclamation sign
#pragma warning disable CS0618 // Type or member is obsolete https://github.com/dotnet/project-system/issues/8591
                    Renamer.RenameDocumentActionSet documentAction = await Renamer.RenameDocumentAsync(oldDocument, null!, documentFolders);
#pragma warning restore CS0618 // Type or member is obsolete

                    if (documentAction.ApplicableActions.IsEmpty ||
                        documentAction.ApplicableActions.Any(a => !a.GetErrors().IsEmpty))
                    {
                        continue;
                    }

                    actions.Add((documentAction, filename));
                }

                return actions;
            }
        }

        public async Task OnAfterFileMoveAsync()
        {
            if (_actions is { Count: not 0 } && await CheckUserConfirmationAsync())
            {
                ApplyNamespaceUpdateActions();
            }

            return;

            async Task<bool> CheckUserConfirmationAsync()
            {
                ISettingsManager settings = await _settingsManagerService.GetValueAsync();

                bool promptNamespaceUpdate = settings.GetValueOrDefault(VsToolsOptions.OptionPromptNamespaceUpdate, true);
                bool enabledNamespaceUpdate = settings.GetValueOrDefault(VsToolsOptions.OptionEnableNamespaceUpdate, true);

                if (!enabledNamespaceUpdate || !promptNamespaceUpdate)
                {
                    return enabledNamespaceUpdate;
                }

                await _projectVsServices.ThreadingService.SwitchToUIThread();

                bool confirmation = _userNotificationServices.Confirm(VSResources.UpdateNamespacePromptMessage, out promptNamespaceUpdate);

                await settings.SetValueAsync(VsToolsOptions.OptionPromptNamespaceUpdate, !promptNamespaceUpdate, true);

                // If the user checked the "Don't show again" checkbox, we need to set the namespace enable state based on their selection of Yes/No in the dialog.
                if (promptNamespaceUpdate)
                {
                    await settings.SetValueAsync(VsToolsOptions.OptionEnableNamespaceUpdate, confirmation, isMachineLocal: true);
                }

                return confirmation;
            }

            void ApplyNamespaceUpdateActions()
            {
                _ = _threadingService.JoinableTaskFactory.RunAsync(async () =>
                {
                    await _projectVsServices.ThreadingService.SwitchToUIThread();

                    string message = _actions.First().Set.ApplicableActions.First().GetDescription();

                    _waitService.Run(
                        title: "",
                        message: message,
                        allowCancel: true,
                        async context =>
                        {
                            await TaskScheduler.Default;

                            Solution solution = await PublishLatestSolutionAsync(context.CancellationToken);

                            int currentStep = 1;

                            foreach ((Renamer.RenameDocumentActionSet action, string fileName) in _actions)
                            {
                                context.Update(currentStep: currentStep++, progressText: fileName);

                                solution = await action.UpdateSolutionAsync(solution, context.CancellationToken);
                            }

                            await _projectVsServices.ThreadingService.SwitchToUIThread();

                            bool applied = _roslynServices.ApplyChangesToSolution(solution.Workspace, solution);

                            System.Diagnostics.Debug.Assert(applied, "ApplyChangesToSolution returned false");
                        },
                        totalSteps: _actions.Count);
                });

                async Task<Solution> PublishLatestSolutionAsync(CancellationToken cancellationToken)
                {
                    // WORKAROUND: We don't yet have a way to wait for the changes to propagate 
                    // to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425), so 
                    // instead we wait for the IntelliSense stage to finish for the entire solution

                    IVsOperationProgressStatusService operationProgressStatusService = await _operationProgressService.GetValueAsync(cancellationToken);

                    IVsOperationProgressStageStatus stageStatus = operationProgressStatusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense);

                    await stageStatus.WaitForCompletionAsync().WithCancellation(cancellationToken);

                    // The result of that wait, is basically a "new" published Solution, so grab it
                    return _workspace.CurrentSolution;
                }
            }
        }
    }
}
