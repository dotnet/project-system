// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Editor;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor.Commands
{
    [Export(typeof(IProjectFileEditorCommandAsync))]
    internal class ProjectFileSaveCommand : IProjectFileEditorCommandAsync
    {
        private readonly IFileSystem _fileSystem;
        private readonly IProjectLockService _projectLockService;
        private readonly IProjectThreadingService _projectThreadingService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IVsEditorAdaptersFactoryService _editorFactoryService;
        private string _lastSavedText;

        [ImportingConstructor]
        public ProjectFileSaveCommand(IFileSystem fileSystem,
            IProjectLockService projectLockService,
            IProjectThreadingService projectThreadingService,
            UnconfiguredProject unconfiguredProject,
            IVsEditorAdaptersFactoryService editorFactoryService)
        {
            _fileSystem = fileSystem;
            _projectLockService = projectLockService;
            _projectThreadingService = projectThreadingService;
            _unconfiguredProject = unconfiguredProject;
            _editorFactoryService = editorFactoryService;
        }

        public long CommandId { get; } = VisualStudioStandard97CommandId.SaveProjectItem;

        public async Task<int> HandleAsync(IVsProject project)
        {
            var projectText = await ReadProjectFileAsync(project).ConfigureAwait(false);

            // Return quick if we haven't made any changes.
            if (projectText.Equals(_lastSavedText))
            {
                return VSConstants.S_OK;
            }

            _lastSavedText = projectText;

            using (var access = await _projectLockService.WriteLockAsync())
            {
                // Ensure the project file is checked out for changes
                await access.CheckoutAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);

                _fileSystem.WriteAllText(_unconfiguredProject.FullPath, projectText);
            }

            await _projectThreadingService.SwitchToUIThread();
            IVsTextLines buffer;
            Verify.HResult(((IVsTextBufferProvider)project).GetTextBuffer(out buffer));
            ITextBuffer textBuffer = _editorFactoryService.GetDocumentBuffer(buffer);
            ITextDocument doc;
            var getResult = textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out doc);
            var eventDelegate = (MulticastDelegate)doc.GetType().GetField("FileActionOccurred", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(doc);
            var fileAction = new TextDocumentFileActionEventArgs(_unconfiguredProject.FullPath, DateTime.Now, FileActionTypes.ContentSavedToDisk);
            foreach (var dlg in eventDelegate.GetInvocationList())
            {
                dlg.Method.Invoke(dlg.Target, new object[] { doc, fileAction });
            }
            return VSConstants.S_OK;
        }

        private async Task<string> ReadProjectFileAsync(IVsProject project)
        {
            await _projectThreadingService.SwitchToUIThread();
            IVsTextLines buffer;
            Verify.HResult(((IVsTextBufferProvider)project).GetTextBuffer(out buffer));
            ITextBuffer textBuffer = _editorFactoryService.GetDocumentBuffer(buffer);
            return textBuffer.CurrentSnapshot.GetText();
        }
    }
}
