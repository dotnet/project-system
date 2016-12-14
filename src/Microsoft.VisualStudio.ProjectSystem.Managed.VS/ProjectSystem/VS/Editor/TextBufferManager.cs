// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TextManager.Interop;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio.Text;
using System;
using Microsoft.VisualStudio.Shell;

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

        public void SetStateFlags(uint flags)
        {
            ((IVsTextBuffer)_textLines).SetStateFlags(flags);
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
            _textBuffer.Replace(textSpan, xmlText);
        }
    }
}
