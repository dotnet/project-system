// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using IServiceProvider = System.IServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    // TODO: Refactor out the state machine into a separate ViewModel. https://github.com/dotnet/roslyn-project-system/issues/836
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemCommandSet, ManagedProjectSystemPackage.EditProjectFileCmdId)]
    [AppliesTo(ProjectCapability.OpenProjectFile)]
    internal class EditProjectFileCommand : AbstractSingleNodeProjectCommand
    {
        private static readonly Guid XmlEditorFactoryGuid = new Guid("{fa3cd31e-987b-443a-9b81-186104e8dac1}");
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMsBuildAccessor _msbuildAccessor;
        private readonly IFileSystem _fileSystem;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly IVsEditorAdaptersFactoryService _editorFactoryService;
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsShellUtilitiesHelper _shellUtilities;
        private readonly ExportFactory<IMsBuildModelWatcher> _watcherFactory;
        private bool _isInitialized;
        private IVsWindowFrame _frame;

        [ImportingConstructor]
        public EditProjectFileCommand(UnconfiguredProject unconfiguredProject,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IMsBuildAccessor msbuildAccessor,
            IFileSystem fileSystem,
            ITextDocumentFactoryService textDocumentService,
            IVsEditorAdaptersFactoryService editorFactoryService,
            IProjectThreadingService threadingService,
            IVsShellUtilitiesHelper shellUtilities,
            ExportFactory<IMsBuildModelWatcher> watcherFactory)
        {
            _unconfiguredProject = unconfiguredProject;
            _serviceProvider = serviceProvider;
            _msbuildAccessor = msbuildAccessor;
            _fileSystem = fileSystem;
            _textDocumentFactoryService = textDocumentService;
            _editorFactoryService = editorFactoryService;
            _threadingService = threadingService;
            _shellUtilities = shellUtilities;
            _watcherFactory = watcherFactory;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus) =>
            ShouldHandle(node) ?
                GetCommandStatusResult.Handled(GetCommandText(node), CommandStatus.Enabled) :
                GetCommandStatusResult.Unhandled;

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, Int64 commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (!ShouldHandle(node)) return false;

            await _threadingService.SwitchToUIThread();

            // If the window has already been opened and hasn't yet been closed, reshow it.
            if (_isInitialized)
            {
                _frame.Show();
                return true;
            }

            Tuple<string, string> fileReturns = await SetupFileAsync().ConfigureAwait(true);
            var tempProjectPath = fileReturns.Item1;
            var lastWrittenXml = fileReturns.Item2;

            IMsBuildModelWatcher watcher = _watcherFactory.CreateExport().Value;
            await watcher.InitializeAsync(tempProjectPath, lastWrittenXml).ConfigureAwait(true);

            // TODO: We shouldn't hardcode the xml editor, as it doesn't respect the user choice for what editor to use.
            // https://github.com/dotnet/roslyn-project-system/issues/871
            _frame = _shellUtilities.OpenDocumentWithSpecificEditor(_serviceProvider, tempProjectPath, XmlEditorFactoryGuid, Guid.Empty);

            // When the document is closed, clean up the file on disk
            var fileCleanupListener = new EditProjectFileVsFrameEvents(tempProjectPath, _fileSystem, watcher, _frame, _serviceProvider, this);
            fileCleanupListener.InitializeEvents();

            // Ensure that the window is not reopened when the solution is closed
            Verify.HResult(_frame.SetProperty((int)__VSFPROPID5.VSFPROPID_DontAutoOpen, true));
            // Set up a save listener, that will overwrite the project file on save.

            _shellUtilities.GetRDTDocumentInfo(_serviceProvider, tempProjectPath, out IVsHierarchy unusedHier, out uint unusedId, out IVsPersistDocData docData, out uint unusedCookie);

            var textBuffer = _editorFactoryService.GetDocumentBuffer((IVsTextBuffer)docData);
            if (!_textDocumentFactoryService.TryGetTextDocument(textBuffer, out ITextDocument textDoc))
            {
                return false;
            }

            Assumes.NotNull(textDoc);
            textDoc.FileActionOccurred += TextDocument_FileActionOccurred;

            Verify.HResult(_frame.Show());

            _isInitialized = true;

            return true;
        }

        /// <summary>
        /// This tells the command that the IVsWindowFrame that was being used for displaying the project file has been closed. This
        /// must be called from the UI thread to avoid races, and will throw if it is not.
        /// </summary>
        internal void Deinit()
        {
            UIThreadHelper.VerifyOnUIThread();
            _isInitialized = false;
            _frame = null;
        }

        private void TextDocument_FileActionOccurred(object sender, TextDocumentFileActionEventArgs args)
        {
            if (args.FileActionType != FileActionTypes.ContentSavedToDisk) return;
            Assumes.Is<ITextDocument>(sender);
            UIThreadHelper.VerifyOnUIThread();
            var textDocument = (ITextDocument)sender;
            var savedText = textDocument.TextBuffer.CurrentSnapshot.GetText();
            _threadingService.ExecuteSynchronously(() => _msbuildAccessor.RunLockedAsync(true, () =>
            {
                _fileSystem.WriteAllText(_unconfiguredProject.FullPath, savedText);
                return Task.CompletedTask;
            }));
        }

        private async Task<Tuple<string, string>> SetupFileAsync()
        {
            var tempProjectPath = GetTempFileName(Path.GetFileName(_unconfiguredProject.FullPath));

            // We don't want to do file IO on the UI thread, so pick up on any old thread and then switch back to the UI thread
            // when we're done the write.
            var lastWrittenXml = await _msbuildAccessor.GetProjectXmlAsync(_unconfiguredProject).ConfigureAwait(false);
            _fileSystem.WriteAllText(tempProjectPath, lastWrittenXml);
            await _threadingService.SwitchToUIThread();
            return Tuple.Create(tempProjectPath, lastWrittenXml);
        }

        private string GetTempFileName(string projectFileName)
        {
            string tempDirectory = _fileSystem.GetTempDirectoryOrFileName();
            _fileSystem.CreateDirectory(tempDirectory);
            return $"{tempDirectory}\\{projectFileName}";
        }

        protected string GetCommandText(IProjectTree node)
        {
            return string.Format(VSResources.EditProjectFileCommand, Path.GetFileName(_unconfiguredProject.FullPath));
        }

        private bool ShouldHandle(IProjectTree node) => node.IsRoot();
    }
}
