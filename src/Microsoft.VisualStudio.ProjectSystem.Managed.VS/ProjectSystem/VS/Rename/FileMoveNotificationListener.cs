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
using Path = System.IO.Path;
using static System.Diagnostics.Debug;

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
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManagerService;

        private readonly Dictionary<string, Renamer.RenameDocumentActionSet> _renameActionSets = new();
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
#pragma warning disable RS0030 // Do not used banned APIs
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
#pragma warning restore RS0030 // Do not used banned APIs
            IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService)
        {
            _unconfiguredProject = unconfiguredProject;
            _userNotificationServices = userNotificationServices;
            _workspace = workspace;
            _threadingService = threadingService;
            _operationProgressService = operationProgressService;
            _waitService = waitService;
            _roslynServices = roslynServices;
            _serviceProvider = serviceProvider;
            _settingsManagerService = settingsManagerService;
        }

        public async Task OnBeforeFilesMovedAsync(IReadOnlyCollection<IFileMoveItem> items)
        {
            // Always start with an empty collection when activated.
            _renameActionSets.Clear();
            Project? project = _workspace.CurrentSolution.Projects.FirstOrDefault(p => StringComparers.Paths.Equals(p.FilePath, _unconfiguredProject.FullPath));
            if (project is null)
            {
                return;
            }

            foreach (IFileMoveItem itemToMove in GetFilesToMoveRecursive(items))
            {
                Document? currentDocument = project.Documents.FirstOrDefault(d => StringComparers.Paths.Equals(d.FilePath, itemToMove.Source));
                if (currentDocument is null)
                {
                    continue;
                }

                // Get the relative folder path from the project to the destination.
                string destinationFolderPath = Path.GetDirectoryName(_unconfiguredProject.MakeRelative(itemToMove.Destination));
                string[] documentFolders = destinationFolderPath.Split(Delimiter.Path, StringSplitOptions.RemoveEmptyEntries);

                // Since this rename only moves the location of the file to another directory, it will use the SyncNamespaceDocumentAction in Roslyn as the rename action within this set.
                // The logic for selecting this rename action can be found here: https://github.com/dotnet/roslyn/blob/960f375f4825a189937d4bfd9fea8162ecc63177/src/Workspaces/Core/Portable/Rename/Renamer.cs#L133-L136
                Renamer.RenameDocumentActionSet renameActionSet = await Renamer.RenameDocumentAsync(currentDocument, s_renameOptions, null, documentFolders);

                if (renameActionSet.ApplicableActions.IsEmpty || renameActionSet.ApplicableActions.Any(a => a.GetErrors().Any()))
                {
                    continue;
                }
                // Getting the rename message requires an instance of Renamer.RenameDocumentAction.
                // We only need to set this message text once for the lifetime of the class, since it isn't dynamic.
                // Even though it isn't dynamic, it does get localized appropriately in Roslyn.
                // The text in English is "Sync namespace to folder structure".
                _renameMessage ??= renameActionSet.ApplicableActions.First().GetDescription();
                _renameActionSets.Add(itemToMove.Destination, renameActionSet);
            }

            return;

            static IEnumerable<IFileMoveItem> GetFilesToMoveRecursive(IEnumerable<IFileMoveItem> items)
            {
                foreach (IFileMoveItem item in items)
                {
                    // Termination condition
                    if (item is { WithinProject: true, IsFolder: false, IsLinked: false } &&
                        StringComparers.ItemTypes.Equals(item.ItemType, Compile.SchemaName))
                    {
                        yield return item;
                        continue;
                    }

                    // Recursive folder navigation
                    if (item is { IsFolder: true } and ICopyPasteItem copyPasteItem)
                    {
                        IEnumerable<IFileMoveItem> children = copyPasteItem.Children.Select(c => c as IFileMoveItem).WhereNotNull();
                        foreach (IFileMoveItem child in GetFilesToMoveRecursive(children))
                        {
                            yield return child;
                        }
                    }
                }
            }
        }

        public async Task OnAfterFileMoveAsync()
        {
            if (!_renameActionSets.Any() || !await IsEnabledOrConfirmedAsync())
            {
                return;
            }

            _ = _threadingService.JoinableTaskFactory.RunAsync(async () =>
            {
                await _threadingService.SwitchToUIThread();
                RunningDocumentTable runningDocumentTable = new(_serviceProvider);
                // Displays a dialog showing the progress of updating the namespaces in the files.
                _waitService.Run(
                    title: string.Empty,
                    message: _renameMessage!,
                    allowCancel: true,
                    asyncMethod: context => ApplyRenamesAsync(context, runningDocumentTable),
                    totalSteps: _renameActionSets.Count);
            });

            return;

            async Task ApplyRenamesAsync(IWaitContext context, RunningDocumentTable runningDocumentTable)
            {
                CancellationToken token = context.CancellationToken;
                await _threadingService.SwitchToUIThread(token);
                foreach (string destinationPath in _renameActionSets.Keys)
                {
                    // Save the current file to disk if it contains unsaved changes.
                    // This guarantees that Roslyn will update the correct document contents when the rename actions are applied to the solution.
                    // This must be done prior to acquiring the latest solution.
                    // For more details, see: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1595580
                    runningDocumentTable.SaveFileIfDirty(destinationPath);
                }

                await TaskScheduler.Default;
                // WORKAROUND: We don't yet have a way to wait for the changes to propagate to Roslyn, tracked by https://github.com/dotnet/project-system/issues/3425
                // Instead, we wait for the IntelliSense stage to finish for the entire solution.
                IVsOperationProgressStatusService statusService = await _operationProgressService.GetValueAsync(token);
                await statusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense).WaitForCompletionAsync().WithCancellation(token);
                // After waiting, a "new" published Solution is available.
                Solution solution = _workspace.CurrentSolution;

                for (int i = 0; i < _renameActionSets.Count; i++)
                {
                    string destinationPath = _renameActionSets.Keys.ElementAt(i);
                    // Display the filename being updated to the user in the progress dialog.
                    context.Update(currentStep: i + 1, progressText: Path.GetFileName(destinationPath));

                    Renamer.RenameDocumentActionSet renameActionSet = _renameActionSets[destinationPath];
                    solution = await renameActionSet.UpdateSolutionAsync(solution, token);
                }

                await _threadingService.SwitchToUIThread(token);
                bool areChangesApplied = _roslynServices.ApplyChangesToSolution(_workspace, solution);
                Assert(areChangesApplied, $"ApplyChangesToSolution returned false");
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
