using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.OLE;
using Microsoft.VisualStudio.Editors.ApplicationDesigner;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using System.Windows.Forms.Design;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;

using OLEInterop = Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Editors.Interop;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    class PropertyPageSiteFake : PropertyPageSite, IPropertyPageSiteInternal, OLEInterop.IServiceProvider
    {
        public ServiceProviderMock Fake_serviceProviderMock = new ServiceProviderMock();
        public UIServiceFake Fake_uiService = new UIServiceFake();
        public VsShellFake Fake_vsShell = new VsShellFake();
        public VsDebuggerFake Fake_vsDebugger = new VsDebuggerFake();
        public VsMonitorSelectionFake Fake_vsMonitorSelection = new VsMonitorSelectionFake();
        public ObjectExtendersFake Fake_objectExtenders = new ObjectExtendersFake();
        public VBEntryPointProviderFake Fake_vbEntryPointProvider = new VBEntryPointProviderFake();
        public VsQueryEditQuerySave2Fake Fake_vsQueryEditQuerySave2 = new VsQueryEditQuerySave2Fake();

        public PropertyPageSiteFake(IPropertyPageSiteOwner view, OleInterop.IPropertyPage page) : base(view, page)
        {
            Fake_serviceProviderMock.Fake_AddService(typeof(OLEInterop.IServiceProvider), this);
            Fake_serviceProviderMock.Fake_AddService(typeof(OLEInterop.IPropertyPageSite), (OLEInterop.IPropertyPageSite)this);
            Fake_serviceProviderMock.Fake_AddService(typeof(IUIService), Fake_uiService);
            Fake_serviceProviderMock.Fake_AddService(typeof(IVsMonitorSelection), Fake_vsMonitorSelection);
            Fake_serviceProviderMock.Fake_AddService(typeof(ObjectExtenders), Fake_objectExtenders);
            Fake_serviceProviderMock.Fake_AddService(typeof(SVsQueryEditQuerySave), Fake_vsQueryEditQuerySave2);

            base.BackingServiceProvider = Fake_serviceProviderMock.Instance;
        }


        #region IPropertyPageSiteInternal Members

        int IPropertyPageSiteInternal.GetLocaleID()
        {
            // Delegate to base PropertyPageSite
            uint localeID;
            ((IPropertyPageSite)this).GetLocaleID(out localeID);
            return (int)localeID;
        }

        object IPropertyPageSiteInternal.GetService(Type serviceType)
        {
            return Fake_serviceProviderMock.Instance.GetService(serviceType);
        }

        bool IPropertyPageSiteInternal.IsImmediateApply
        {
            get
            {
                // Always true for modeless property pages
                return true;
            }
        }

        void IPropertyPageSiteInternal.OnStatusChange(Microsoft.VisualStudio.Editors.PropertyPages.PROPPAGESTATUS flags)
        {
            // Delegate to base PropertyPageSite
            ((IPropertyPageSite)this).OnStatusChange((uint)flags);
        }

        int IPropertyPageSiteInternal.TranslateAccelerator(System.Windows.Forms.Message msg)
        {
            return Microsoft.VisualStudio.Editors.Interop.NativeMethods.S_FALSE;
        }

        #endregion

        #region OLE.Interop.IServiceProvider Members

        int Microsoft.VisualStudio.OLE.Interop.IServiceProvider.QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            if (guidService.Equals(typeof(IUIService).GUID))
            {
                ppvObject = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(Fake_uiService);
                return VSConstants.S_OK;
            }
            if (guidService.Equals(typeof(IVsShell).GUID))
            {
                ppvObject = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(Fake_vsShell);
                return VSConstants.S_OK;
            }
            if (guidService.Equals(typeof(IVsDebugger).GUID))
            {
                ppvObject = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(Fake_vsDebugger);
                return VSConstants.S_OK;
            }
            if (guidService.Equals(typeof(ObjectExtenders).GUID))
            {
                ppvObject = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(Fake_objectExtenders);
                return VSConstants.S_OK;
            }
            if (guidService.Equals(Interop.NativeMethods.VBCompilerGuid))
            {
                ppvObject = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(Fake_vbEntryPointProvider);
                return VSConstants.S_OK;
            }
            if (guidService.Equals(typeof(SVsQueryEditQuerySave).GUID))
            {
                ppvObject = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(Fake_vsQueryEditQuerySave2);
                return VSConstants.S_OK;
            }
            if (guidService.Equals(typeof(System.ComponentModel.Design.Serialization.CodeDomSerializer).GUID))
            {
                ppvObject = IntPtr.Zero;
                return VSConstants.E_NOINTERFACE;
            }

            System.Diagnostics.Debug.Fail("QueryService: Specified guid not implemented");
            throw new Exception("QueryService: Specified guid not implemented");
        }

        #endregion


#if false
        #region OLEInterop.IPropertyPageSite Members

        void OLEInterop.IPropertyPageSite.GetLocaleID(out uint pLocaleID)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void OLEInterop.IPropertyPageSite.GetPageContainer(out object ppunk)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void OLEInterop.IPropertyPageSite.OnStatusChange(uint dwFlags)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int OLEInterop.IPropertyPageSite.TranslateAccelerator(OLEInterop.MSG[] pMsg)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IServiceProvider Members

        object IServiceProvider.GetService(Type serviceType)
        {
            return Fake_serviceProviderMock.Instance.GetService(serviceType);
        }

        #endregion

#endif

    }
}
