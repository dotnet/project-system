using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOLEProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Guid(EditorFactoryGuid)]
    class LoadedProjectFileEditorFactory : IVsEditorFactory
    {
        public const string EditorFactoryGuid = "da07c581-c7b4-482a-86fe-39aacfe5ca5c";
        public static readonly Guid XmlEditorFactoryGuid = new Guid("{fa3cd31e-987b-443a-9b81-186104e8dac1}");
        private readonly IServiceProvider _serviceProvider;
        private IVsEditorFactory _xmlEditorFactory;

        public LoadedProjectFileEditorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Int32 Close()
        {
            throw new NotImplementedException();
        }

        public Int32 CreateEditorInstance(UInt32 grfCreateDoc, String pszMkDocument, String pszPhysicalView, IVsHierarchy pvHier, UInt32 itemid, IntPtr punkDocDataExisting, out IntPtr ppunkDocView, out IntPtr ppunkDocData, out String pbstrEditorCaption, out Guid pguidCmdUI, out Int32 pgrfCDW)
        {
            Requires.NotNull(_xmlEditorFactory, nameof(_xmlEditorFactory));
            Int32 result = _xmlEditorFactory.CreateEditorInstance(grfCreateDoc, pszMkDocument, pszPhysicalView, pvHier, itemid, punkDocDataExisting, out ppunkDocView, out ppunkDocData, out pbstrEditorCaption, out pguidCmdUI, out pgrfCDW);
            if (result == VSConstants.S_OK)
            {
                var punkView = Marshal.GetObjectForIUnknown(ppunkDocView);
                var viewWindow = punkView as WindowPane;
                if (viewWindow != null)
                {
                    var wrapper = new XmlEditorWrapper(viewWindow);
                    ppunkDocView = Marshal.GetIUnknownForObject(wrapper);
                }
            }
            return result;
        }

        public Int32 MapLogicalView(ref Guid rguidLogicalView, out String pbstrPhysicalView)
        {
            var shellOpenDocument = _serviceProvider.GetService<IVsUIShellOpenDocument, SVsUIShellOpenDocument>();
            pbstrPhysicalView = null;
            if (shellOpenDocument == null)
            {
                return VSConstants.E_UNEXPECTED;
            }

            String unusedPhysicalView;
            Verify.HResult(shellOpenDocument.GetStandardEditorFactory(0, XmlEditorFactoryGuid, null, rguidLogicalView, out unusedPhysicalView, out _xmlEditorFactory));
            return _xmlEditorFactory.MapLogicalView(rguidLogicalView, out pbstrPhysicalView);
        }

        public Int32 SetSite(IOLEProvider unused)
        {
            return VSConstants.S_OK;
        }
    }
}
