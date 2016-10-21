// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TextManager.Interop;
using System;
using IServiceProvider = System.IServiceProvider;
using System.ComponentModel.Composition;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export(ExportContractNames.VsTypes.ProjectNodeComExtension)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [ComServiceIid(typeof(IVsTextBufferProvider))]
    internal class IVsTextBufferProviderProjectAggregator : OnceInitializedOnceDisposed, IVsTextBufferProvider
    {
        private static readonly Guid XmlEditorFactory = Guid.Parse("{fa3cd31e-987b-443a-9b81-186104e8dac1}");

        private readonly IServiceProvider _serviceProvider;
        private readonly RunningDocumentTable _rdt;
        private readonly IFileSystem _fileSystem;
        private readonly IUnconfiguredProjectVsServices _vsServices;
        private readonly IProjectThreadingService _threadingService;
        private readonly IProjectLockService _projectLockService;
        private IVsTextLines _textBufferAdapter;
        private IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private IContentTypeRegistryService _contentTypeRegistryService;
        private IComponentModel _componentModel;

        [ImportingConstructor]
        public IVsTextBufferProviderProjectAggregator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IUnconfiguredProjectVsServices vsServices,
            IFileSystem fileSystem,
            IProjectThreadingService threadingService,
            IProjectLockService projectLockService)
        {
            _serviceProvider = serviceProvider;
            _rdt = new RunningDocumentTable(_serviceProvider);
            _fileSystem = fileSystem;
            _vsServices = vsServices;
            _threadingService = threadingService;
            _projectLockService = projectLockService;
        }

        protected override void Initialize()
        {
            UIThreadHelper.VerifyOnUIThread();
            string projectXml = _threadingService.ExecuteSynchronously(GetProjectXml);

            var oleServiceProvder = _serviceProvider.GetService<IOleServiceProvider>();

            _componentModel = _serviceProvider.GetService<IComponentModel, SComponentModel>();
            _editorAdaptersFactoryService = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _contentTypeRegistryService = _componentModel.GetService<IContentTypeRegistryService>();
            var contentType = _contentTypeRegistryService.GetContentType("XML");
            _textBufferAdapter = _editorAdaptersFactoryService.CreateVsTextBufferAdapter(oleServiceProvder, contentType) as IVsTextLines;
            _textBufferAdapter.InitializeContent(projectXml, projectXml.Length);
        }

        protected async Task<string> GetProjectXml()
        {
            using (var access = await _projectLockService.ReadLockAsync())
            {
                // Need to return on the same thread as the lock was aquired on.
                var xmlProject = await access.GetProjectAsync(_vsServices.ActiveConfiguredProject).ConfigureAwait(true);
                return xmlProject.Xml.RawXml;
            }
        }

        protected override void Dispose(Boolean initialized)
        {
            // Nothing to dispose of.
        }

        public Int32 GetTextBuffer(out IVsTextLines ppTextBuffer)
        {
            EnsureInitialized(true);
            ppTextBuffer = _textBufferAdapter;
            return VSConstants.S_OK;
        }

        public Int32 SetTextBuffer(IVsTextLines pTextBuffer)
        {
            _textBufferAdapter = pTextBuffer;
            return VSConstants.S_OK;
        }

        public Int32 LockTextBuffer(Int32 fLock)
        {
            return VSConstants.S_OK;
        }
    }
}
