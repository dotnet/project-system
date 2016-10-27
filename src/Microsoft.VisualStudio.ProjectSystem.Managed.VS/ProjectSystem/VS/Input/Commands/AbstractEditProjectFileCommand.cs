// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractEditProjectFileCommand : AbstractSingleNodeProjectCommand
    {
        private static readonly Guid XmlEditorFactoryGuid = new Guid("{fa3cd31e-987b-443a-9b81-186104e8dac1}");
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IProjectCapabilitiesService _projectCapabiltiesService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IProjectLockService _projectLockService;
        private readonly IFileSystem _fileSystem;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly IVsEditorAdaptersFactoryService _editorFactoryService;

        public AbstractEditProjectFileCommand(IUnconfiguredProjectVsServices projectVsServices,
            IProjectCapabilitiesService projectCapabilitiesService,
            IServiceProvider serviceProvider,
            IProjectLockService projectLockService,
            IFileSystem fileSystem,
            ITextDocumentFactoryService textDocumentService,
            IVsEditorAdaptersFactoryService editorFactoryService)
        {
            _projectVsServices = projectVsServices;
            _projectCapabiltiesService = projectCapabilitiesService;
            _serviceProvider = serviceProvider;
            _projectLockService = projectLockService;
            _fileSystem = fileSystem;
            _textDocumentFactoryService = textDocumentService;
            _editorFactoryService = editorFactoryService;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, Boolean focused, String commandText, CommandStatus progressiveStatus) =>
            ShouldHandle(node) ?
                GetCommandStatusResult.Handled(GetCommandText(node), CommandStatus.Enabled) :
                GetCommandStatusResult.Unhandled;

        protected override async Task<Boolean> TryHandleCommandAsync(IProjectTree node, Boolean focused, Int64 commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (!ShouldHandle(node)) return false;

            var caption = Path.GetFileName(_projectVsServices.Project.FullPath);

            var projPath = await GetFile().ConfigureAwait(false);
            await _projectVsServices.ThreadingService.SwitchToUIThread();
            IVsWindowFrame frame;

            frame = VsShellUtilities.OpenDocumentWithSpecificEditor(_serviceProvider, projPath, XmlEditorFactoryGuid, Guid.Empty);

            // Clean up the caption of the window
            frame.SetProperty((int)__VSFPROPID5.VSFPROPID_OverrideCaption, caption);

            // Set up a save listener, that will overwrite the project file on save.
            IVsHierarchy unusedHier;
            uint unusedId;
            uint unusedCookie;
            IVsPersistDocData docData;

            VsShellUtilities.GetRDTDocumentInfo(_serviceProvider, projPath, out unusedHier, out unusedId, out docData, out unusedCookie);

            var textBuffer = _editorFactoryService.GetDocumentBuffer(docData as IVsTextBuffer);
            ITextDocument textDoc;
            if (!_textDocumentFactoryService.TryGetTextDocument(textBuffer, out textDoc))
            {
                return false;
            }

            // Save the project file location now so the node isn't captured by the lambda
            var projectFile = _projectVsServices.Project.FullPath;
            textDoc.FileActionOccurred += (sender, args) =>
            {
                // We're only interested in saves.
                if (args.FileActionType != FileActionTypes.ContentSavedToDisk) return;
                Assumes.Is<ITextDocument>(sender);
                var textDocument = (ITextDocument)sender;
                var savedText = textDocument.TextBuffer.CurrentSnapshot.GetText();
                _fileSystem.WriteAllText(projectFile, savedText);
            };

            frame.Show();

            return true;
        }

        private async Task<string> GetFile()
        {
            var projectXml = await GetProjectXml().ConfigureAwait(false);
            var tempFileName = $"{_fileSystem.GetTempFileName()}.{FileExtension}";
            _fileSystem.WriteAllText(tempFileName, projectXml);
            return tempFileName;
        }

        private async Task<string> GetProjectXml()
        {
            var configuredProject = _projectVsServices.ActiveConfiguredProject;
            using (var access = await _projectLockService.ReadLockAsync())
            {
                var xmlProject = await access.GetProjectAsync(configuredProject).ConfigureAwait(true);
                return xmlProject.Xml.RawXml;
            }
        }

        protected string GetCommandText(IProjectTree node)
        {
            return string.Format(VSResources.EditProjectFileCommand, node.Caption, FileExtension);
        }

        protected abstract string FileExtension { get; }

        private bool ShouldHandle(IProjectTree node) => node.IsRoot() && _projectCapabiltiesService.Contains("OpenProjectFile");
    }
}
