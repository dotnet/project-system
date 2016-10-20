// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using IServiceProvider = System.IServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal partial class XmlEditorWrapper : IServiceProvider, IVsWindowPane, IVsUIElementPane, IDisposable
    {
        public int ClosePane()
        {
            return ((IVsWindowPane)_delegatePane).ClosePane();
        }

        public int CloseUIElementPane()
        {
            return ((IVsUIElementPane)_delegatePane).CloseUIElementPane();
        }

        public int CreatePaneWindow(IntPtr hwndParent, int x, int y, int cx, int cy, out IntPtr hwnd)
        {
            return ((IVsWindowPane)_delegatePane).CreatePaneWindow(hwndParent, x, y, cx, cy, out hwnd);
        }

        public int CreateUIElementPane(out object punkUIElement)
        {
            return ((IVsUIElementPane)_delegatePane).CreateUIElementPane(out punkUIElement);
        }

        public void Dispose()
        {
            _delegatePane.Dispose();
        }

        public int GetDefaultSize(SIZE[] pSize)
        {
            return ((IVsWindowPane)_delegatePane).GetDefaultSize(pSize);
        }

        public int GetDefaultUIElementSize(SIZE[] psize)
        {
            return ((IVsUIElementPane)_delegatePane).GetDefaultUIElementSize(psize);
        }

        public object GetService(Type serviceType)
        {
            return ((IServiceProvider)_delegatePane).GetService(serviceType);
        }

        public int LoadUIElementState(IStream pstream)
        {
            return ((IVsUIElementPane)_delegatePane).LoadUIElementState(pstream);
        }

        public int LoadViewState(IStream pStream)
        {
            return ((IVsWindowPane)_delegatePane).LoadViewState(pStream);
        }

        public int SaveUIElementState(IStream pstream)
        {
            return ((IVsUIElementPane)_delegatePane).SaveUIElementState(pstream);
        }

        public int SaveViewState(IStream pStream)
        {
            return ((IVsWindowPane)_delegatePane).SaveViewState(pStream);
        }

        public int SetSite(OLE.Interop.IServiceProvider psp)
        {
            return ((IVsWindowPane)_delegatePane).SetSite(psp);
        }

        public int SetUIElementSite(OLE.Interop.IServiceProvider pSP)
        {
            return ((IVsUIElementPane)_delegatePane).SetUIElementSite(pSP);
        }

        public int TranslateAccelerator(MSG[] lpmsg)
        {
            return ((IVsWindowPane)_delegatePane).TranslateAccelerator(lpmsg);
        }

        public int TranslateUIElementAccelerator(MSG[] lpmsg)
        {
            return ((IVsUIElementPane)_delegatePane).TranslateUIElementAccelerator(lpmsg);
        }
    }
}
