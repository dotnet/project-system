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
using Microsoft.VisualStudio.Text;

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
        private readonly IOleServiceProvider _oleProvider;
        private IVsTextLines _textBufferAdapter;
        private IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private IContentTypeRegistryService _contentTypeRegistryService;
        private IComponentModel _componentModel;

        [ImportingConstructor]
        public IVsTextBufferProviderProjectAggregator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IUnconfiguredProjectVsServices vsServices,
            IFileSystem fileSystem)
        {
            _serviceProvider = serviceProvider;
            _rdt = new RunningDocumentTable(_serviceProvider);
            _fileSystem = fileSystem;
            _vsServices = vsServices;
            _oleProvider = new ServiceProviderToOleServiceProviderAdapter(_serviceProvider);
        }

        protected override void Initialize()
        {
            UIThreadHelper.VerifyOnUIThread();
            var text = _fileSystem.ReadAllText(_vsServices.Project.FullPath);
            var oleServiceProvder = _serviceProvider.GetService<IOleServiceProvider>();

            _componentModel = _serviceProvider.GetService<IComponentModel, SComponentModel>();
            _editorAdaptersFactoryService = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _contentTypeRegistryService = _componentModel.GetService<IContentTypeRegistryService>();
            var contentType = _contentTypeRegistryService.GetContentType("XML");
            _textBufferAdapter = _editorAdaptersFactoryService.CreateVsTextBufferAdapter(oleServiceProvder, contentType) as IVsTextLines;
            _textBufferAdapter.InitializeContent(text, text.Length);
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
            return VSConstants.E_NOTIMPL;
        }
    }
}
