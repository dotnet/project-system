// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.ProjectSystem.VS.Build;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    /// <summary>
    /// This class manages access to the IVsTextBuffer used by the in-memory project file editor.
    /// </summary>
    [Export(typeof(ITextBufferManager))]
    internal class TextBufferManager : OnceInitializedOnceDisposed, ITextBufferManager
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersService;
        private readonly IContentTypeRegistryService _contentTypeService;
        private readonly IMsBuildAccessor _msbuildAccessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly IProjectThreadingService _threadingService;
        private readonly UnconfiguredProject _unconfiguredProject;

        private IVsTextLines _textLines;
        private ITextBuffer _textBuffer;

        [ImportingConstructor]
        public TextBufferManager(IVsEditorAdaptersFactoryService editorAdaptersService,
            IContentTypeRegistryService contentTypeService,
            IMsBuildAccessor msbuildAccessor,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IProjectThreadingService threadingService,
            UnconfiguredProject unconfiguredProject) :
            base(synchronousDisposal: true)
        {
            _editorAdaptersService = editorAdaptersService;
            _contentTypeService = contentTypeService;
            _msbuildAccessor = msbuildAccessor;
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
            _unconfiguredProject = unconfiguredProject;
        }

        public IVsTextLines TextLines
        {
            get
            {
                EnsureInitialized(true);
                return _textLines;
            }
        }

        public ITextBuffer TextBuffer
        {
            get
            {
                EnsureInitialized(true);
                return _textBuffer;
            }
        }

        public async Task SetReadOnlyAsync(bool readOnly)
        {
            await _threadingService.SwitchToUIThread();
            var vsTextBuffer = (IVsTextBuffer)_textLines;
            Verify.HResult(vsTextBuffer.GetStateFlags(out uint oldFlags));
            var newFlags = readOnly ? oldFlags | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY :
                oldFlags & ~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY;
            Verify.HResult(vsTextBuffer.SetStateFlags(newFlags));
        }


        protected override void Initialize()
        {
            UIThreadHelper.VerifyOnUIThread();
            var contentType = _contentTypeService.GetContentType("XML");
            var oleServiceProvider = _serviceProvider.GetService<IOleServiceProvider>();
            _textLines = (IVsTextLines)_editorAdaptersService.CreateVsTextBufferAdapter(oleServiceProvider, contentType);
            var xmlText = _threadingService.ExecuteSynchronously(_msbuildAccessor.GetProjectXmlAsync);
            _textLines.InitializeContent(xmlText, xmlText.Length);
            _textBuffer = _editorAdaptersService.GetDocumentBuffer(_textLines);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _textLines = null;
                _textBuffer = null;
            }
        }

        public void ResetBuffer()
        {
            EnsureInitialized(true);
            UIThreadHelper.VerifyOnUIThread();
            var xmlText = _threadingService.ExecuteSynchronously(_msbuildAccessor.GetProjectXmlAsync);
            var textSpan = new Span(0, _textBuffer.CurrentSnapshot.Length);

            // When the buffer is being reset, it's often set to ReadOnly. Turn it off while we edit, and then turn it back on after the edit is finished.
            // We're on the UI thread at this point, so the user can't make any edits anyway.
            var vsTextBuffer = (IVsTextBuffer)_textLines;
            Verify.HResult(vsTextBuffer.GetStateFlags(out uint oldFlags));
            vsTextBuffer.SetStateFlags(oldFlags & ~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);

            _textBuffer.Replace(textSpan, xmlText);

            // Restore the old flags after the edit has been made.
            vsTextBuffer.SetStateFlags(oldFlags);
        }
    }
}
