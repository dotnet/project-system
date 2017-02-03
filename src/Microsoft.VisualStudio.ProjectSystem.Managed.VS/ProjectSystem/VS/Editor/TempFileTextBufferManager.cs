// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export(typeof(ITextBufferManager))]
    internal class TempFileTextBufferManager : OnceInitializedOnceDisposedAsync, ITextBufferManager
    {
        private readonly Regex _whitespaceRegex = new Regex(@"\s");

        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IProjectXmlAccessor _projectXmlAccessor;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersService;
        private readonly ITextDocumentFactoryService _textDocumentService;
        private readonly IVsShellUtilitiesHelper _shellUtilities;
        private readonly IFileSystem _fileSystem;
        private readonly IProjectThreadingService _threadingService;
        private readonly IServiceProvider _serviceProvider;

        private IVsTextBuffer _textBuffer;
        private ITextDocument _textDocument;
        private IVsPersistDocData _docData;

        [ImportingConstructor]
        public TempFileTextBufferManager(UnconfiguredProject unconfiguredProject,
            IProjectXmlAccessor projectXmlAccessor,
            IVsEditorAdaptersFactoryService editorAdaptersService,
            ITextDocumentFactoryService textDocumentService,
            IVsShellUtilitiesHelper shellUtilities,
            IFileSystem fileSystem,
            IProjectThreadingService threadingService,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider) :
            base(threadingService != null ? threadingService.JoinableTaskContext : throw new ArgumentNullException(nameof(threadingService)))
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(projectXmlAccessor, nameof(projectXmlAccessor));
            Requires.NotNull(editorAdaptersService, nameof(editorAdaptersService));
            Requires.NotNull(textDocumentService, nameof(textDocumentService));
            Requires.NotNull(shellUtilities, nameof(shellUtilities));
            Requires.NotNull(fileSystem, nameof(fileSystem));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));

            _unconfiguredProject = unconfiguredProject;
            _projectXmlAccessor = projectXmlAccessor;
            _editorAdaptersService = editorAdaptersService;
            _textDocumentService = textDocumentService;
            _shellUtilities = shellUtilities;
            _fileSystem = fileSystem;
            _threadingService = threadingService;
            _serviceProvider = serviceProvider;
        }

        public string FilePath { get; set; }

        public Task InitializeBufferAsync() => InitializeAsync();

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            FilePath = GetTempFileName(Path.GetFileName(_unconfiguredProject.FullPath));

            var projectXml = await _projectXmlAccessor.GetProjectXmlAsync().ConfigureAwait(false);
            _fileSystem.WriteAllText(FilePath, projectXml, await _unconfiguredProject.GetFileEncodingAsync().ConfigureAwait(false));
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _fileSystem.RemoveDirectory(Path.GetDirectoryName(FilePath), true);
            return Task.CompletedTask;
        }

        public async Task ResetBufferAsync()
        {
            var projectXml = await _projectXmlAccessor.GetProjectXmlAsync().ConfigureAwait(false);
            var existingText = await ReadBufferXmlAsync().ConfigureAwait(false);

            if (!existingText.Equals(projectXml, StringComparison.Ordinal))
            {
                await _threadingService.SwitchToUIThread();
                // If the docdata is not dirty, we just update the buffer to avoid the file reload pop-up. Otherwise,
                // we write to disk, to force the pop-up.
                Verify.HResult(_docData.IsDocDataDirty(out int isDirty));
                if (Convert.ToBoolean(isDirty))
                {
                    _fileSystem.WriteAllText(FilePath, projectXml, await _unconfiguredProject.GetFileEncodingAsync().ConfigureAwait(true));
                }
                else
                {
                    var textSpan = new Span(0, _textDocument.TextBuffer.CurrentSnapshot.Length);

                    // When the buffer is being reset, it's often been set to ReadOnly. We can't update the buffer when it's readonly, so save
                    // the currently flags and turn off ReadOnly, restoring after we're done. We're on the UI thread, so this is invisible to
                    // the user.
                    Verify.HResult(_textBuffer.GetStateFlags(out uint oldFlags));
                    _textBuffer.SetStateFlags(oldFlags & ~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);

                    // Make sure that the buffer is reset back to the old flags, regardless of TextBuffer.Replace throwing any kind of exception
                    try
                    {
                        _textDocument.TextBuffer.Replace(textSpan, projectXml);
                        // We save the DocData here so that if the user decides to Undo the change from the file itself, the editor will be considered
                        // dirty and will prompt about unsaved changes if closed before saving again.
                        _docData.SaveDocData(VSSAVEFLAGS.VSSAVE_Save, out string unused, out int cancelled);
                    }
                    finally
                    {
                        var isReadonly = (oldFlags & (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY) == (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY;
                        if (isReadonly)
                        {
                            Verify.HResult(_textBuffer.GetStateFlags(out uint newFlags));
                            _textBuffer.SetStateFlags(newFlags | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);
                        }
                    }
                }
            }

            // Since we're writing to the file on disk in all scenarios, we can potentially race with the project reload mechanism during project
            // close if the file on disk has divergent changes from the temp file. This causes a series of confusing popups for the user that make
            // it very likely they will accidentally discard changes. We save here to make sure we don't run into that race.
            await _unconfiguredProject.SaveAsync().ConfigureAwait(false);
        }

        public async Task SaveAsync()
        {
            var toWrite = await ReadBufferXmlAsync().ConfigureAwait(false);
            await _projectXmlAccessor.SaveProjectXmlAsync(toWrite).ConfigureAwait(false);
        }

        public async Task SetReadOnlyAsync(bool readOnly)
        {
            await _threadingService.SwitchToUIThread();

            // If we've never fetched the buffer before, then we need to find the buffer holding the data.
            if (_textBuffer == null)
            {
                await InitializeTextBufferAsync().ConfigureAwait(true);
            }

            Assumes.NotNull(_textBuffer);

            // Ensure we only clear/set the BSF_USER_READONLY flag, and don't touch the other flags.
            Verify.HResult(_textBuffer.GetStateFlags(out uint oldFlags));
            var newFlags = readOnly ?
                oldFlags | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY :
                oldFlags & ~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY;
            Verify.HResult(_textBuffer.SetStateFlags(newFlags));
        }

        private string GetTempFileName(string projectFileName)
        {
            string tempDirectory = _fileSystem.GetTempDirectoryOrFileName();
            _fileSystem.CreateDirectory(tempDirectory);
            return $"{tempDirectory}\\{projectFileName}";
        }

        private async Task InitializeTextBufferAsync()
        {
            (IVsHierarchy unusedHier, uint unusedId, IVsPersistDocData docData, uint unusedCookie) =
                await _shellUtilities.GetRDTDocumentInfoAsync(_serviceProvider, FilePath).ConfigureAwait(false);
            _docData = docData;

            await _threadingService.SwitchToUIThread();
            _textBuffer = (IVsTextBuffer)_docData;

            var textBufferAdapter = _editorAdaptersService.GetDocumentBuffer(_textBuffer);
            Assumes.True(_textDocumentService.TryGetTextDocument(textBufferAdapter, out _textDocument));
        }

        private async Task<string> ReadBufferXmlAsync()
        {
            await _threadingService.SwitchToUIThread();
            if (_textDocument == null)
            {
                await InitializeTextBufferAsync().ConfigureAwait(true);
            }

            Assumes.NotNull(_textDocument);
            return _textDocument.TextBuffer.CurrentSnapshot.GetText();
        }
    }
}
