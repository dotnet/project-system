// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Remoting.Contexts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Path = System.IO.Path;
//using RunningDocumentTable = Microsoft.VisualStudio.Shell.RunningDocumentTable;
using static System.Diagnostics.Debug;
using System.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [Order(Order.Default)]
    [Export(typeof(IFileMoveNotificationListener))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class FileMoveNotificationListener : IFileMoveNotificationListener
    {
        private static readonly DocumentRenameOptions s_renameOptions = new();

        //private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsService<SVsOperationProgress, IVsOperationProgressStatusService> _operationProgressService;
        private readonly IWaitIndicator _waitService;
        private readonly IRoslynServices _roslynServices;
        private readonly IVsService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManagerService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersService;

        //private List<(Action<Renamer.RenameDocumentActionSet> SetAction, string FileName)>? _actions;
        private readonly Dictionary<string, Renamer.RenameDocumentActionSet> _renameActions = new();
        //private readonly Dictionary<string, (string Destination, SourceText Text)> _sourceTexts = new();

        //private readonly Dictionary<string, (Document Document, SourceText Text)> _sourceTexts = new();

        [ImportingConstructor]
        public FileMoveNotificationListener(
            //UnconfiguredProject unconfiguredProject,
            IUserNotificationServices userNotificationServices,
            IUnconfiguredProjectVsServices projectVsServices,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace,
            IProjectThreadingService threadingService,
            IVsService<SVsOperationProgress, IVsOperationProgressStatusService> operationProgressService,
            IWaitIndicator waitService,
            IRoslynServices roslynServices,
            IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService,
#pragma warning disable RS0030 // Do not used banned APIs
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
#pragma warning restore RS0030 // Do not used banned APIs
            IVsEditorAdaptersFactoryService editorAdaptersService)
        {
            //_unconfiguredProject = unconfiguredProject;
            _userNotificationServices = userNotificationServices;
            _projectVsServices = projectVsServices;
            _workspace = workspace;
            _threadingService = threadingService;
            _operationProgressService = operationProgressService;
            _waitService = waitService;
            _roslynServices = roslynServices;
            _settingsManagerService = settingsManagerService;
            _serviceProvider = serviceProvider;
            _editorAdaptersService = editorAdaptersService;
        }

        public async Task OnBeforeFilesMovedAsync(IReadOnlyCollection<IFileMoveItem> items)
        {
            //_sourceTexts.Clear();
            _renameActions.Clear();
            Project? project = _workspace.CurrentSolution.Projects
                .FirstOrDefault(p => StringComparers.Paths.Equals(p.FilePath, _projectVsServices.Project.FullPath));

            if (project is null)
            {
                return;
            }

            //IEnumerable<IFileMoveItem> itemsToMove = ;
            //foreach (IFileMoveItem item in items)
            //{
            //    IEnumerable<IFileMoveItem> itemsToMove = GetFilesToMoveRecursive(item);
            //}
            //_actions = GetNamespaceUpdateActionsAsync;

            // FOR THE RUNNING DOCUMENT TABLE
            //await _projectVsServices.ThreadingService.SwitchToUIThread();

            foreach (IFileMoveItem itemToMove in GetFilesToMoveRecursive(items))
            {
                Document? currentDocument = project.Documents.FirstOrDefault(d => StringComparers.Paths.Equals(d.FilePath, itemToMove.Source));
                if (currentDocument is null)
                {
                    continue;
                }

                ////string destinationFileRelative = PathHelper.MakeRelative(Path.GetDirectoryName(project.FilePath), itemToMove.Destination);

                // Get the relative folder path from the project to the destination.
                string destinationFolderPath = Path.GetDirectoryName(_projectVsServices.Project.MakeRelative(itemToMove.Destination));
                string[] documentFolders = destinationFolderPath.Split(Delimiter.Path, StringSplitOptions.RemoveEmptyEntries);

                // This is a file item to another directory, it should only detect this a Update Namespace action.
                Renamer.RenameDocumentActionSet renameAction = await Renamer.RenameDocumentAsync(currentDocument, s_renameOptions, null, documentFolders);

                if (renameAction.ApplicableActions.IsEmpty || renameAction.ApplicableActions.Any(a => a.GetErrors().Any()))
                {
                    continue;
                }

                _renameActions.Add(itemToMove.Source, renameAction);







                //RunningDocumentTable runningDocumentTable = new(_serviceProvider);
                //RunningDocumentInfo documentInfo = runningDocumentTable.GetDocumentInfo(itemToMove.Source);
                //// Indicates that the document exists in the table.
                //if (documentInfo.DocCookie != VSConstants.VSCOOKIE_NIL &&
                //    documentInfo.DocData is IVsTextBuffer vsTextBuffer &&
                //    _editorAdaptersService.GetDocumentBuffer(vsTextBuffer) is ITextBuffer textBuffer)
                //{
                //    //_sourceTexts.Add(itemToMove.Source, (itemToMove.Destination, textBuffer.CurrentSnapshot.AsText()));
                //    _sourceTexts.Add(itemToMove.Destination, (currentDocument, textBuffer.CurrentSnapshot.AsText()));
                //}
            }

            //else
            //{
            //    _actions = null;
            //}

            return;

            static IEnumerable<IFileMoveItem> GetFilesToMoveRecursive(IEnumerable<IFileMoveItem> items)
            {
                foreach(IFileMoveItem item in items)
                {
                    // Termination part
                    if (item is { WithinProject: true, IsFolder: false, IsLinked: false } &&
                        StringComparers.ItemTypes.Equals(item.ItemType, Compile.SchemaName))
                    {
                        yield return item;
                        continue;
                    }

                    // Recursive part
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
            if (_renameActions.Any() && await CheckUserConfirmationAsync())
            {
                //ApplyNamespaceUpdateActions();

                _ = _threadingService.JoinableTaskFactory.RunAsync(async () =>
                {
                    await _projectVsServices.ThreadingService.SwitchToUIThread();

                    //var pair = _renameActions.First();
                    //var filename = pair.FileName;
                    //var actionSet = pair.Set;


                    //var action = _renameActions.First().Value.ApplicableActions.First();
                    //var errors = action.GetErrors();

                    // All this is done to simply get a message for the wait service.
                    // TODO: Simply put a message there...
                    //string message = _renameActions.First().Value.ApplicableActions.First().GetDescription();

                    //Project? project = _workspace.CurrentSolution.Projects
                    //    .FirstOrDefault(p => StringComparers.Paths.Equals(p.FilePath, _projectVsServices.Project.FullPath));

                    //if (project is null)
                    //{
                    //    return;
                    //}



                    _waitService.Run(
                        title: "",
                        // TODO: Temporary
                        message: "Sync namespace to folder structure",
                        allowCancel: true,
                        asyncMethod: RunRenameActions,
                        totalSteps: _renameActions.Count);
                });
            }

            return;

            async Task RunRenameActions(IWaitContext context)
            {
                await TaskScheduler.Default;

                // WORKAROUND: We don't yet have a way to wait for the changes to propagate 
                // to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425), so 
                // instead we wait for the IntelliSense stage to finish for the entire solution
                IVsOperationProgressStatusService statusService = await _operationProgressService.GetValueAsync(context.CancellationToken);
                IVsOperationProgressStageStatus stageStatus = statusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense);
                await stageStatus.WaitForCompletionAsync().WithCancellation(context.CancellationToken);
                // The result of that wait, is basically a "new" published Solution, so grab it
                Solution solution = _workspace.CurrentSolution;

                //int currentStep = 1;

                //foreach ((string destinationPath, (Document document, SourceText originalText)) in _sourceTexts)
                //{
                //    Renamer.RenameDocumentActionSet? renameAction = await CreateRenamerAction(destinationPath, originalText);
                //    if (renameAction is null)
                //    {
                //        continue;
                //    }

                //    context.Update(currentStep: currentStep++, progressText: Path.GetFileName(destinationPath));

                //    solution = await renameAction.UpdateSolutionAsync(solution, context.CancellationToken);
                //}

                //foreach ((string sourcePath, Renamer.RenameDocumentActionSet renameAction) in _renameActions)
                //{
                //    context.Update(currentStep: currentStep++, progressText: Path.GetFileName(sourcePath));

                //    solution = await renameAction.UpdateSolutionAsync(solution, context.CancellationToken);
                //}

                for (int i = 0; i < _renameActions.Count; i++)
                {
                    string sourcePath = _renameActions.Keys.ElementAt(i);
                    Renamer.RenameDocumentActionSet renameAction = _renameActions.Values.ElementAt(i);
                    context.Update(currentStep: i + 1, progressText: Path.GetFileName(sourcePath));
                    solution = await renameAction.UpdateSolutionAsync(solution, context.CancellationToken);
                }

                await _projectVsServices.ThreadingService.SwitchToUIThread();
                bool applied = _roslynServices.ApplyChangesToSolution(solution.Workspace, solution);
                Assert(applied, "ApplyChangesToSolution returned false");
            }

            async Task<bool> CheckUserConfirmationAsync()
            {
                ISettingsManager settings = await _settingsManagerService.GetValueAsync();

                bool enabledNamespaceUpdate = settings.GetValueOrDefault(VsToolsOptions.OptionEnableNamespaceUpdate, true);
                bool promptNamespaceUpdate = settings.GetValueOrDefault(VsToolsOptions.OptionPromptNamespaceUpdate, true);
                if (!enabledNamespaceUpdate || !promptNamespaceUpdate)
                {
                    return enabledNamespaceUpdate;
                }

                await _projectVsServices.ThreadingService.SwitchToUIThread();
                bool confirmation = _userNotificationServices.Confirm(VSResources.UpdateNamespacePromptMessage, out promptNamespaceUpdate);
                await settings.SetValueAsync(VsToolsOptions.OptionPromptNamespaceUpdate, !promptNamespaceUpdate, true);
                return confirmation;
            }

            //void ApplyNamespaceUpdateActions()
            //{
                

                //return;

                //async Task<Renamer.RenameDocumentActionSet?> CreateRenamerAction(string destinationPath, SourceText originalText)
                //{
                //    //Document? currentDocument = project.Documents.FirstOrDefault(d => StringComparers.Paths.Equals(d.FilePath, sourcePath));
                //    //Document? renamedDocument = project.Documents.FirstOrDefault(d => d.Id.Equals(id));
                //    //if (renamedDocument is null)
                //    //{
                //    //    return null;
                //    //}
                //    Project? project = _workspace.CurrentSolution.Projects
                //        .FirstOrDefault(p => StringComparers.Paths.Equals(p.FilePath, _projectVsServices.Project.FullPath));

                //    if (project is null)
                //    {
                //        return null;
                //    }


                //    Document? newDocument = project.Documents.FirstOrDefault(d => StringComparers.Paths.Equals(d.FilePath, destinationPath));
                //    if (newDocument is null)
                //    {
                //        return null;
                //    }

                //    // Replace the renamed document's text with original text prior to updating the namespace.
                //    newDocument = newDocument.WithText(originalText);
                //    //_roslynServices.ApplyChangesToSolution(document.Project.Solution.Workspace, document.Project.Solution);

                //    //string destinationFileRelative = PathHelper.MakeRelative(Path.GetDirectoryName(project.FilePath), itemToMove.Destination);

                //    // Get the relative folder path from the project to the destination.
                //    string destinationFolderPath = Path.GetDirectoryName(_projectVsServices.Project.MakeRelative(destinationPath));
                //    string[] documentFolders = destinationFolderPath.Split(Delimiter.Path, StringSplitOptions.RemoveEmptyEntries);

                //    // This is a file item to another directory, it should only detect this a Update Namespace action.
                //    Renamer.RenameDocumentActionSet renameAction = await Renamer.RenameDocumentAsync(newDocument, s_renameOptions, null, documentFolders);

                //    if (renameAction.ApplicableActions.IsEmpty || renameAction.ApplicableActions.Any(a => a.GetErrors().Any()))
                //    {
                //        return null;
                //    }

                //    return renameAction;
                //}

                //async Task<Solution> PublishLatestSolutionAsync(CancellationToken cancellationToken)
                //{
                //    // WORKAROUND: We don't yet have a way to wait for the changes to propagate 
                //    // to Roslyn (tracked by https://github.com/dotnet/project-system/issues/3425), so 
                //    // instead we wait for the IntelliSense stage to finish for the entire solution

                //    IVsOperationProgressStatusService operationProgressStatusService = await _operationProgressService.GetValueAsync(cancellationToken);

                //    IVsOperationProgressStageStatus stageStatus = operationProgressStatusService.GetStageStatus(CommonOperationProgressStageIds.Intellisense);

                //    await stageStatus.WaitForCompletionAsync().WithCancellation(cancellationToken);

                //    // The result of that wait, is basically a "new" published Solution, so grab it
                //    return _workspace.CurrentSolution;
                //}
            //}
        }
    }
}
