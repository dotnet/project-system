// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
// Debug collides with Microsoft.VisualStudio.ProjectSystem.VS.Debug
using DiagDebug = System.Diagnostics.Debug;
using Path = System.IO.Path;
using static Microsoft.CodeAnalysis.Rename.Renamer;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [Order(Order.Default)]
    [Export(typeof(IFileMoveNotificationListener))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class FileMoveNotificationListener : IFileMoveNotificationListener
    {
        private static readonly DocumentRenameOptions s_renameOptions = new();

        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsService<SVsOperationProgress, IVsOperationProgressStatusService> _operationProgressService;
        private readonly IWaitIndicator _waitService;
        private readonly IRoslynServices _roslynServices;
        private readonly IVsService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManagerService;

        // The file-paths are the full disk path of the source file (path prior to moving the item).
        private readonly Dictionary<string, RenameDocumentActionSet> _renameActionSetByFilePath = new();
        private string? _renameMessage;

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
            _workspace = workspace;
            _threadingService = threadingService;
            _operationProgressService = operationProgressService;
            _waitService = waitService;
            _roslynServices = roslynServices;
            _settingsManagerService = settingsManagerService;
        }

        public async Task OnBeforeFilesMovedAsync(IReadOnlyCollection<IFileMoveItem> items)
        {
            Project? project = _workspace.CurrentSolution.Projects.FirstOrDefault(p => StringComparers.Paths.Equals(p.FilePath, _unconfiguredProject.FullPath));
            if (project is null)
            {
                return;
            }

            foreach (IFileMoveItem itemToMove in GetFilesToMove(items))
            {
                Document? currentDocument = project.Documents.FirstOrDefault(d => StringComparers.Paths.Equals(d.FilePath, itemToMove.Source));
                if (currentDocument is null)
                {
                    continue;
                }

                // Get the relative folder path from the project to the destination.
                string destinationFolderPath = Path.GetDirectoryName(_unconfiguredProject.MakeRelative(itemToMove.Destination));
                string[] destinationFolders = destinationFolderPath.Split(Delimiter.Path, StringSplitOptions.RemoveEmptyEntries);

                // Since this rename only moves the location of the file to another directory, it will use the SyncNamespaceDocumentAction in Roslyn as the rename action within this set.
                // The logic for selecting this rename action can be found here: https://github.com/dotnet/roslyn/blob/960f375f4825a189937d4bfd9fea8162ecc63177/src/Workspaces/Core/Portable/Rename/Renamer.cs#L133-L136
                RenameDocumentActionSet renameActionSet = await RenameDocumentAsync(currentDocument, s_renameOptions, null, destinationFolders);
                if (renameActionSet.ApplicableActions.IsEmpty || renameActionSet.ApplicableActions.Any(aa => aa.GetErrors().Any()))
                {
                    continue;
                }

                // Getting the rename message requires an instance of RenameDocumentAction.
                // We only need to set this message text once for the lifetime of the class, since it isn't dynamic.
                // Even though it isn't dynamic, it does get localized appropriately in Roslyn.
                // The text in English is "Sync namespace to folder structure".
                _renameMessage ??= renameActionSet.ApplicableActions.First().GetDescription();

                // Add the full source file-path of the item as the key for the rename action set.
                _renameActionSetByFilePath.Add(itemToMove.Source, renameActionSet);
            }

            return;

            static IEnumerable<IFileMoveItem> GetFilesToMove(IEnumerable<IFileMoveItem> items)
            {
                var itemQueue = new Queue<IFileMoveItem>(items);
                while(itemQueue.Count > 0)
                {
                    IFileMoveItem item = itemQueue.Dequeue();

                    // Termination condition
                    if (item is { WithinProject: true, IsFolder: false, IsLinked: false } &&
                        StringComparers.ItemTypes.Equals(item.ItemType, Compile.SchemaName))
                    {
                        yield return item;
                        continue;
                    }

                    // Folder navigation
                    if (item is { IsFolder: true } and ICopyPasteItem copyPasteItem)
                    {
                        IEnumerable<IFileMoveItem> children = copyPasteItem.Children.Select(c => c as IFileMoveItem).WhereNotNull();
                        foreach (IFileMoveItem child in children)
                        {
                            itemQueue.Enqueue(child);
                        }
                    }
                }
            }
        }

        public async Task OnAfterFileMoveAsync()
        {
            if (!_renameActionSetByFilePath.Any() || !await IsEnabledOrConfirmedAsync())
            {
                // Clear the collection since the user declined (or has disabled) the rename namespace option.
                _renameActionSetByFilePath.Clear();
                return;
            }

            // Display a dialog showing the progress of updating the namespaces in the files.
            _ = _waitService.RunAsync(
                title: string.Empty,
                message: _renameMessage!,
                allowCancel: true,
                asyncMethod: ApplyRenamesAsync,
                totalSteps: _renameActionSetByFilePath.Count);

            return;

            async Task ApplyRenamesAsync(IWaitContext context)
            {
                CancellationToken token = context.CancellationToken;
                await TaskScheduler.Default;
                // WORKAROUND: We don't yet have a way to wait for the changes to propagate to Roslyn, tracked by https://github.com/dotnet/project-system/issues/3425
                // Instead, we wait for the IntelliSense stage to finish for the entire solution.
                IVsOperationProgressStatusService statusService = await _operationProgressService.GetValueAsync(token);
                await statusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense).WaitForCompletionAsync().WithCancellation(token);
                // After waiting, a "new" published Solution is available.
                Solution solution = _workspace.CurrentSolution;

                int currentStep = 1;
                foreach ((string filePath, RenameDocumentActionSet renameActionSet) in _renameActionSetByFilePath)
                {
                    // Display the filename being updated to the user in the progress dialog.
                    context.Update(currentStep: currentStep++, progressText: Path.GetFileName(filePath));

                    solution = await renameActionSet.UpdateSolutionAsync(solution, token);
                }

                await _threadingService.SwitchToUIThread(token);
                bool areChangesApplied = _roslynServices.ApplyChangesToSolution(_workspace, solution);
                DiagDebug.Assert(areChangesApplied, "ApplyChangesToSolution returned false");
                // Clear the collection after it has been processed.
                _renameActionSetByFilePath.Clear();
            }

            async Task<bool> IsEnabledOrConfirmedAsync()
            {
                ISettingsManager settings = await _settingsManagerService.GetValueAsync();

                bool isEnabled = settings.GetValueOrDefault(VsToolsOptions.OptionEnableNamespaceUpdate, defaultValue: true);
                bool isPromptEnabled = settings.GetValueOrDefault(VsToolsOptions.OptionPromptNamespaceUpdate, defaultValue: true);
                // If not enabled, returns false.
                // If enabled but prompt is not enabled, returns true.
                // Otherwise, we display the prompt to the user.
                if (!isEnabled || !isPromptEnabled)
                {
                    return isEnabled;
                }

                await _threadingService.SwitchToUIThread();
                bool isConfirmed = _userNotificationServices.Confirm(VSResources.UpdateNamespacePromptMessage, out bool disablePromptMessage);
                await settings.SetValueAsync(VsToolsOptions.OptionPromptNamespaceUpdate, !disablePromptMessage, isMachineLocal: true);
                // If the user checked the "Don't show again" checkbox, we need to set the namespace enable state based on their selection of Yes/No in the dialog.
                if (disablePromptMessage)
                {
                    await settings.SetValueAsync(VsToolsOptions.OptionEnableNamespaceUpdate, isConfirmed, isMachineLocal: true);
                }
                return isConfirmed;
            }
        }
    }
}
