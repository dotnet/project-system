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
        private readonly IVsService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManagerService;

        private readonly Dictionary<string, Renamer.RenameDocumentActionSet> _renameActions = new();
        private string? _renameMessage;
        private int _assertCount = 0;
        private int _runCount = 0;
        private int _afterCount = 0;

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

        //public async Task OnBeforeFilesMovedAsync(IReadOnlyCollection<IFileMoveItem> items)
        //{
        //    // Always start with an empty actions collection when activated.
        //    _renameActions.Clear();
        //    Project? project = _workspace.CurrentSolution.Projects.FirstOrDefault(p => StringComparers.Paths.Equals(p.FilePath, _unconfiguredProject.FullPath));
        //    if (project is null)
        //    {
        //        return;
        //    }

        //    foreach (IFileMoveItem itemToMove in GetFilesToMoveRecursive(items))
        //    {
        //        Document? currentDocument = project.Documents.FirstOrDefault(d => StringComparers.Paths.Equals(d.FilePath, itemToMove.Source));
        //        if (currentDocument is null)
        //        {
        //            continue;
        //        }

        //        // Get the relative folder path from the project to the destination.
        //        string destinationFolderPath = Path.GetDirectoryName(_unconfiguredProject.MakeRelative(itemToMove.Destination));
        //        string[] documentFolders = destinationFolderPath.Split(Delimiter.Path, StringSplitOptions.RemoveEmptyEntries);

        //        // This is a file item to another directory, it should only detect this a Update Namespace action.
        //        Renamer.RenameDocumentActionSet renameAction = await Renamer.RenameDocumentAsync(currentDocument, s_renameOptions, null, documentFolders);

        //        if (renameAction.ApplicableActions.IsEmpty || renameAction.ApplicableActions.Any(a => a.GetErrors().Any()))
        //        {
        //            continue;
        //        }
        //        // Getting the rename message requires an instance of Renamer.RenameDocumentAction.
        //        // We only need to set this message text once for the lifetime of the class, since it isn't dynamic.
        //        // Even though it isn't dynamic, it does get localized appropriately in Roslyn.
        //        // The text in English is "Sync namespace to folder structure".
        //        _renameMessage ??= renameAction.ApplicableActions.First().GetDescription();
        //        _renameActions.Add(itemToMove.Source, renameAction);
        //    }

        //    return;

        //    static IEnumerable<IFileMoveItem> GetFilesToMoveRecursive(IEnumerable<IFileMoveItem> items)
        //    {
        //        foreach(IFileMoveItem item in items)
        //        {
        //            // Termination condition
        //            if (item is { WithinProject: true, IsFolder: false, IsLinked: false } &&
        //                StringComparers.ItemTypes.Equals(item.ItemType, Compile.SchemaName))
        //            {
        //                yield return item;
        //                continue;
        //            }

        //            // Recursive folder navigation
        //            if (item is { IsFolder: true } and ICopyPasteItem copyPasteItem)
        //            {
        //                IEnumerable<IFileMoveItem> children = copyPasteItem.Children.Select(c => c as IFileMoveItem).WhereNotNull();
        //                foreach (IFileMoveItem child in GetFilesToMoveRecursive(children))
        //                {
        //                    yield return child;
        //                }
        //            }
        //        }
        //    }
        //}

        public async Task OnBeforeFilesMovedAsync(IReadOnlyCollection<IFileMoveItem> items)
        {
            // Always start with an empty actions collection when activated.
            _renameActions.Clear();
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

                // This is a file item to another directory, it should only detect this a Update Namespace action.
                Renamer.RenameDocumentActionSet renameAction = await Renamer.RenameDocumentAsync(currentDocument, s_renameOptions, null, documentFolders);

                if (renameAction.ApplicableActions.IsEmpty || renameAction.ApplicableActions.Any(a => a.GetErrors().Any()))
                {
                    continue;
                }
                // Getting the rename message requires an instance of Renamer.RenameDocumentAction.
                // We only need to set this message text once for the lifetime of the class, since it isn't dynamic.
                // Even though it isn't dynamic, it does get localized appropriately in Roslyn.
                // The text in English is "Sync namespace to folder structure".
                _renameMessage ??= renameAction.ApplicableActions.First().GetDescription();
                _renameActions.Add(itemToMove.Source, renameAction);
            }

            _ = _threadingService.JoinableTaskFactory.RunAsync(async () =>
            {
                //_runCount++;

                //CancellationToken token = new();
                //_runCount++;
                //var areChangesApplied = await RunRenameActions(token);
                //_afterCount++;

                //if (!areChangesApplied)
                //{
                //    Assert(areChangesApplied, $"ApplyChangesToSolution returned false: r:{_runCount} as:{_assertCount++} af:{_afterCount}");
                //}

                await _threadingService.SwitchToUIThread();
                // Displays a dialog showing the progress of updating the namespaces in the files.
                _waitService.Run(
                    title: string.Empty,
                    message: _renameMessage!,
                    allowCancel: true,
                    asyncMethod: RunRenameActions,
                    totalSteps: _renameActions.Count);
            });

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

            async Task RunRenameActions(IWaitContext context)
            {
                CancellationToken token = context.CancellationToken;
                await TaskScheduler.Default;

                // WORKAROUND: We don't yet have a way to wait for the changes to propagate to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425).
                // Instead, we wait for the IntelliSense stage to finish for the entire solution.
                IVsOperationProgressStatusService statusService = await _operationProgressService.GetValueAsync(token);
                await statusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense).WaitForCompletionAsync().WithCancellation(token);
                // After waiting, a "new" published Solution is available.
                Solution solution = _workspace.CurrentSolution;
                //await _threadingService.SwitchToUIThread(token);
                //_roslynServices.ApplyChangesToSolution(_workspace, solution);

                for (int i = 0; i < _renameActions.Count; i++)
                {
                    string sourcePath = _renameActions.Keys.ElementAt(i);
                    Renamer.RenameDocumentActionSet renameAction = _renameActions.Values.ElementAt(i);
                    //context.Update(currentStep: i + 1, progressText: Path.GetFileName(sourcePath));
                    solution = await renameAction.UpdateSolutionAsync(solution, token);
                }

                await _threadingService.SwitchToUIThread(token);
                //return _roslynServices.ApplyChangesToSolution(_workspace, solution);

                bool areChangesApplied = _roslynServices.ApplyChangesToSolution(_workspace, solution);
                if (!areChangesApplied)
                {
                    Assert(areChangesApplied, $"ApplyChangesToSolution returned false: {_assertCount++}");
                }
            }
        }

        public async Task OnAfterFileMoveAsync()
        {
            return;
        }

        public async Task OnAfterFileMove2Async()
        {
            if (!_renameActions.Any() || !await IsEnabledOrConfirmedAsync())
            {
                return;
            }

            //await _threadingService.SwitchToUIThread();
            //// Displays a dialog showing the progress of updating the namespaces in the files.
            //_waitService.Run(
            //    title: string.Empty,
            //    message: _renameMessage!,
            //    allowCancel: true,
            //    asyncMethod: RunRenameActions,
            //    totalSteps: _renameActions.Count);

            _ = _threadingService.JoinableTaskFactory.RunAsync(async () =>
            {
                //_runCount++;

                CancellationToken token = new();
                _runCount++;
                var areChangesApplied = await RunRenameActions(token);
                _afterCount++;

                if (!areChangesApplied)
                {
                    Assert(areChangesApplied, $"ApplyChangesToSolution returned false: r:{_runCount} as:{_assertCount++} af:{_afterCount}");
                }

                //await _threadingService.SwitchToUIThread();
                //// Displays a dialog showing the progress of updating the namespaces in the files.
                //_waitService.Run(
                //    title: string.Empty,
                //    message: _renameMessage!,
                //    allowCancel: true,
                //    asyncMethod: RunRenameActions,
                //    totalSteps: _renameActions.Count);
            });

            return;

            //async Task RunRenameActions(IWaitContext context)
            //{
            //    await TaskScheduler.Default;

            //    // WORKAROUND: We don't yet have a way to wait for the changes to propagate to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425).
            //    // Instead, we wait for the IntelliSense stage to finish for the entire solution.
            //    IVsOperationProgressStatusService statusService = await _operationProgressService.GetValueAsync(context.CancellationToken);
            //    await statusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense).WaitForCompletionAsync().WithCancellation(context.CancellationToken);
            //    // After waiting, a "new" published Solution is available.
            //    Solution solution = _workspace.CurrentSolution;

            //    for (int i = 0; i < _renameActions.Count; i++)
            //    {
            //        string sourcePath = _renameActions.Keys.ElementAt(i);
            //        Renamer.RenameDocumentActionSet renameAction = _renameActions.Values.ElementAt(i);
            //        context.Update(currentStep: i + 1, progressText: Path.GetFileName(sourcePath));
            //        solution = await renameAction.UpdateSolutionAsync(solution, context.CancellationToken);
            //    }

            //    await _threadingService.SwitchToUIThread(cancellationToken: context.CancellationToken);
            //    context.Update(currentStep: _renameActions.Count);
            //    bool areChangesApplied = _roslynServices.ApplyChangesToSolution(_workspace, solution);
            //    //bool areChangesApplied = _workspace.TryApplyChanges(solution);
            //    //if (areChangesApplied)
            //    //    return;

            //    //bool areChangesApplied = false;
            //    //// Attempt 3 times since there is a potential misalignment of solution state.
            //    //for (int i = 0; i < 3; i++)
            //    //{
            //    //    areChangesApplied = _workspace.TryApplyChanges(solution);
            //    //    if (areChangesApplied)
            //    //        return;

            //    //    await TaskScheduler.Default;
            //    //    // WORKAROUND: We don't yet have a way to wait for the changes to propagate to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425).
            //    //    // Instead, we wait for the IntelliSense stage to finish for the entire solution.
            //    //    statusService = await _operationProgressService.GetValueAsync(token);
            //    //    await statusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense).WaitForCompletionAsync().WithCancellation(token);
            //    //    await _threadingService.SwitchToUIThread();
            //    //}
            //    Assert(areChangesApplied, "ApplyChangesToSolution returned false");
            //}

            async Task<bool> RunRenameActions(CancellationToken token)
            {
                //CancellationToken token = context.CancellationToken;
                await TaskScheduler.Default;

                // WORKAROUND: We don't yet have a way to wait for the changes to propagate to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425).
                // Instead, we wait for the IntelliSense stage to finish for the entire solution.
                IVsOperationProgressStatusService statusService = await _operationProgressService.GetValueAsync(token);
                await statusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense).WaitForCompletionAsync().WithCancellation(token);
                // After waiting, a "new" published Solution is available.
                Solution solution = _workspace.CurrentSolution;
                //await _threadingService.SwitchToUIThread(token);
                //_roslynServices.ApplyChangesToSolution(_workspace, solution);

                for (int i = 0; i < _renameActions.Count; i++)
                {
                    string sourcePath = _renameActions.Keys.ElementAt(i);
                    Renamer.RenameDocumentActionSet renameAction = _renameActions.Values.ElementAt(i);
                    //context.Update(currentStep: i + 1, progressText: Path.GetFileName(sourcePath));
                    solution = await renameAction.UpdateSolutionAsync(solution, token);
                }

                await _threadingService.SwitchToUIThread(token);
                return _roslynServices.ApplyChangesToSolution(_workspace, solution);
                //bool areChangesApplied = _roslynServices.ApplyChangesToSolution(_workspace, solution);
                //bool areChangesApplied = _workspace.TryApplyChanges(solution);
                //if (areChangesApplied)
                //    return;

                //bool areChangesApplied = false;
                //// Attempt 3 times since there is a potential misalignment of solution state.
                //for (int i = 0; i < 3; i++)
                //{
                //    areChangesApplied = _workspace.TryApplyChanges(solution);
                //    if (areChangesApplied)
                //        return;

                //    Thread.Sleep(2000);
                //}
                //if (!areChangesApplied)
                //{
                //    Assert(areChangesApplied, $"ApplyChangesToSolution returned false: {_assertCount++}");
                //}
                //_renameActions.Clear();
            }

            //async Task<bool> RunRenameActions(CancellationToken token)
            //{
            //    await TaskScheduler.Default;

            //    // WORKAROUND: We don't yet have a way to wait for the changes to propagate to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425).
            //    // Instead, we wait for the IntelliSense stage to finish for the entire solution.
            //    IVsOperationProgressStatusService statusService = await _operationProgressService.GetValueAsync(token);
            //    await statusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense).WaitForCompletionAsync().WithCancellation(token);
            //    // After waiting, a "new" published Solution is available.
            //    Solution solution = _workspace.CurrentSolution;

            //    for (int i = 0; i < _renameActions.Count; i++)
            //    {
            //        string sourcePath = _renameActions.Keys.ElementAt(i);
            //        Renamer.RenameDocumentActionSet renameAction = _renameActions.Values.ElementAt(i);
            //        //context.Update(currentStep: i + 1, progressText: Path.GetFileName(sourcePath));
            //        solution = await renameAction.UpdateSolutionAsync(solution, token);
            //    }

            //    await _threadingService.SwitchToUIThread();
            //    return _roslynServices.ApplyChangesToSolution(_workspace, solution);
            //    //bool areChangesApplied = _roslynServices.ApplyChangesToSolution(_workspace, solution);
            //    //bool areChangesApplied = _workspace.TryApplyChanges(solution);
            //    //if (areChangesApplied)
            //    //    return;

            //    //bool areChangesApplied = false;
            //    //// Attempt 3 times since there is a potential misalignment of solution state.
            //    //for (int i = 0; i < 3; i++)
            //    //{
            //    //    areChangesApplied = _workspace.TryApplyChanges(solution);
            //    //    if (areChangesApplied)
            //    //        return;

            //    //    Thread.Sleep(2000);
            //    //}
            //    //if (!areChangesApplied)
            //    //{
            //    //    Assert(areChangesApplied, $"ApplyChangesToSolution returned false: {_assertCount++}");
            //    //}
            //    //_renameActions.Clear();
            //}

            //async Task RunRenameActions(IWaitContext context)
            //{
            //    await TaskScheduler.Default;

            //    // WORKAROUND: We don't yet have a way to wait for the changes to propagate to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425).
            //    // Instead, we wait for the IntelliSense stage to finish for the entire solution.
            //    IVsOperationProgressStatusService statusService = await _operationProgressService.GetValueAsync(context.CancellationToken);
            //    await statusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense).WaitForCompletionAsync().WithCancellation(context.CancellationToken);
            //    // After waiting, a "new" published Solution is available.
            //    Solution solution = _workspace.CurrentSolution;

            //    for (int i = 0; i < _renameActions.Count; i++)
            //    {
            //        string sourcePath = _renameActions.Keys.ElementAt(i);
            //        Renamer.RenameDocumentActionSet renameAction = _renameActions.Values.ElementAt(i);
            //        context.Update(currentStep: i + 1, progressText: Path.GetFileName(sourcePath));
            //        solution = await renameAction.UpdateSolutionAsync(solution, context.CancellationToken);
            //    }

            //    await _threadingService.SwitchToUIThread();
            //    bool areChangesApplied = _roslynServices.ApplyChangesToSolution(solution.Workspace, solution);
            //    Assert(areChangesApplied, "ApplyChangesToSolution returned false");
            //}

            async Task<bool> IsEnabledOrConfirmedAsync()
            {
                ISettingsManager settings = await _settingsManagerService.GetValueAsync();

                bool isEnabled = settings.GetValueOrDefault(VsToolsOptions.OptionEnableNamespaceUpdate, defaultValue: true);
                bool isPromptEnabled = settings.GetValueOrDefault(VsToolsOptions.OptionPromptNamespaceUpdate, defaultValue: true);
                // If not enabled, returns false.
                // If enabled but prompt not enabled, returns true.
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
