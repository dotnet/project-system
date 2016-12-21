// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    public abstract partial class PropertyPage : UserControl,
       IPropertyPage,
       IVsDebuggerEvents
    {
        private IPropertyPageSite _site = null;
        private bool _isDirty = false;
        private bool _ignoreEvents = false;
        private bool _useJoinableTaskFactory = true;
        private IVsDebugger _debugger;
        private uint _debuggerCookie;
        internal IProjectThreadingService _threadHandling;

        // WIN32 Constants
        private const int
            WM_KEYFIRST = 0x0100,
            WM_KEYLAST = 0x0108,
            WM_MOUSEFIRST = 0x0200,
            WM_MOUSELAST = 0x020A,
            SW_HIDE = 0;

        internal static class NativeMethods
        {
            public const int
                S_OK = 0x00000000;
        }

        protected abstract string PropertyPageName { get; }

        internal PropertyPage()
        {
            AutoScroll = false;
        }

        // For unit testing
        internal PropertyPage(bool useJoinableTaskFactory)
        {
            _useJoinableTaskFactory = useJoinableTaskFactory;
        }

        internal UnconfiguredProject UnconfiguredProject { get; set; }
        
        
        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// Property. Gets or sets whether the page is dirty. Dirty status is pushed to owner property sheet
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                // Only process real changes
                if (value != _isDirty && !_ignoreEvents)
                {
                    _isDirty = value;
                    // If dirty, this causes Apply to be called
                    if (_site != null)
                        _site.OnStatusChange((uint)(_isDirty ? PROPPAGESTATUS.PROPPAGESTATUS_DIRTY : PROPPAGESTATUS.PROPPAGESTATUS_CLEAN));
                }
            }
        }

        public List<IVsBrowseObjectContext> ContextObjects { get; private set; }

        /// <summary>
        /// Helper to wait on async tasks
        /// </summary>
        private T WaitForAsync<T>(Func<Task<T>> asyncFunc)
        {
            return _threadHandling.ExecuteSynchronously<T>(asyncFunc);
        }

        /// <summary>
        /// Helper to wait on async tasks
        /// </summary>
        private void WaitForAsync(Func<Task> asyncFunc)
        {
            _threadHandling.ExecuteSynchronously(asyncFunc);
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IPropertyPage
        /// This is called before our form is shown but after SetObjects is called.
        /// This is the place from which the form can populate itself using the information available
        /// in CPS.
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        public void Activate(IntPtr hWndParent, RECT[] pRect, int bModal)
        {
            AdviseDebugger();
            SuspendLayout();
            // Initialization can cause some events to be fired when we change some values
            // so we use this flag (_ignoreEvents) to notify IsDirty to ignore
            // any changes that happen during initialization
            Win32Methods.SetParent(Handle, hWndParent);
            ResumeLayout();

        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IPropertyPage
        /// This is where the information entered in the form should be saved in CPS
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        public int Apply()
        {
            return WaitForAsync<int>(OnApply);
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IPropertyPage
        /// Called when the page is deactivated
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        public void Deactivate()
        {
            WaitForAsync(OnDeactivate);
            UnadviseDebugger();
            Dispose(true);
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IPropertyPage
        /// Returns a stuct describing our property page
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        public void GetPageInfo(PROPPAGEINFO[] pPageInfo)
        {
            PROPPAGEINFO info = new PROPPAGEINFO();

            info.cb = (uint)Marshal.SizeOf(typeof(PROPPAGEINFO));
            info.dwHelpContext = 0;
            info.pszDocString = null;
            info.pszHelpFile = null;
            info.pszTitle = PropertyPageName;
            // set the size to 0 so the host doesn't use scroll bars
            // we want to do that within our own container.
            info.SIZE.cx = 0;
            info.SIZE.cy = 0;
            if (pPageInfo != null && pPageInfo.Length > 0)
                pPageInfo[0] = info;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IPropertyPage
        /// Returns the help context
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        public void Help(string pszHelpDir)
        {
            return;
        }

        
        public int IsPageDirty()
        {
            if (IsDirty)
                return VSConstants.S_OK;
            return VSConstants.S_FALSE;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IPropertyPage
        ///  Called when the page is moved or sized
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        public new void Move(Microsoft.VisualStudio.OLE.Interop.RECT[] pRect)
        {
            if (pRect == null || pRect.Length <= 0)
                throw new ArgumentNullException("pRect");

            Microsoft.VisualStudio.OLE.Interop.RECT r = pRect[0];

            Location = new Point(r.left, r.top);
            Size = new Size(r.right - r.left, r.bottom - r.top);
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// Notification that debug mode changed
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        public int OnModeChange(DBGMODE dbgmodeNew)
        {
            Enabled = (dbgmodeNew == DBGMODE.DBGMODE_Design);
            return NativeMethods.S_OK;
        }
        
        /// <summary>
        /// Informs derived classes that configuration has changed
        /// </summary>
        internal void SetObjects(bool isClosing)
        {
            WaitForAsync(async () => await OnSetObjects(isClosing).ConfigureAwait(false));
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IPropertyPage
        /// Site for our page
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        public void SetPageSite(IPropertyPageSite pPageSite)
        {
            _site = pPageSite;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IPropertyPage
        /// Show/Hide the page
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        public void Show(uint nCmdShow)
        {
            if (nCmdShow != SW_HIDE)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IPropertyPage
        /// Handles mneumonics
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        public int TranslateAccelerator(MSG[] pMsg)
        {
            if (pMsg == null)
                return VSConstants.E_POINTER;

            System.Windows.Forms.Message m = System.Windows.Forms.Message.Create(pMsg[0].hwnd, (int)pMsg[0].message, pMsg[0].wParam, pMsg[0].lParam);
            bool used = false;

            // Preprocessing should be passed to the control whose handle the message refers to.
            Control target = Control.FromChildHandle(m.HWnd);
            if (target != null)
                used = target.PreProcessMessage(ref m);

            if (used)
            {
                pMsg[0].message = (uint)m.Msg;
                pMsg[0].wParam = m.WParam;
                pMsg[0].lParam = m.LParam;
                // Returning S_OK indicates we handled the message ourselves
                return VSConstants.S_OK;
            }


            // Returning S_FALSE indicates we have not handled the message
            int result = 0;
            if (_site != null)
                result = _site.TranslateAccelerator(pMsg);
            return result;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// Initialize and listen to debug mode changes
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        internal void AdviseDebugger()
        {
            if (_site is System.IServiceProvider sp)
            {
                _debugger = sp.GetService<IVsDebugger, IVsDebugger>();
                if (_debugger != null)
                {
                    _debugger.AdviseDebuggerEvents(this, out _debuggerCookie);
                    DBGMODE[] dbgMode = new DBGMODE[1];
                    _debugger.GetMode(dbgMode);
                    ((IVsDebuggerEvents)this).OnModeChange(dbgMode[0]);
                }
            }
        }


        /// <summary>
        /// Get the unconfigured property provider for the project
        /// </summary>
        internal virtual UnconfiguredProject GetUnconfiguredProject(IVsHierarchy hier)
        {
            var provider = GetExport<IProjectExportProvider>(hier);
            return provider.GetExport<UnconfiguredProject>(hier.GetDTEProject().FileName);
        }

        /// <summary>
        /// Get export
        /// </summary>
        internal static T GetExport<T>(IVsHierarchy hier)
        {
            System.IServiceProvider sp = new Microsoft.VisualStudio.Shell.ServiceProvider((OLE.Interop.IServiceProvider)hier.GetDTEProject().DTE);
            IComponentModel compMode = sp.GetService<IComponentModel, SComponentModel>();
            return compMode.DefaultExportProvider.GetExport<T>().Value;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// Quit listening to debug mode changes
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        private void UnadviseDebugger()
        {
            if (_debuggerCookie != 0 && _debugger != null)
            {
                _debugger.UnadviseDebuggerEvents(_debuggerCookie);
            }
            _debugger = null;
            _debuggerCookie = 0;
        }
        protected abstract Task<int> OnApply();
        protected abstract Task OnDeactivate();
        protected abstract Task OnSetObjects(bool isClosing);

        public void SetObjects(uint cObjects, object[] ppunk)
        {
            UnconfiguredProject = null;
            if (cObjects == 0)
            {
                // If we have never configured anything (maybe a failure occurred on open so app designer is closing us). In this case
                // do nothing
                if (_threadHandling != null)
                {
                    SetObjects(true);
                }
                return;
            }

            if (ppunk.Length < cObjects)
                throw new ArgumentOutOfRangeException("cObjects");

            List<string> configurations = new List<string>();
            // Look for an IVsBrowseObject
            for (int i = 0; i < cObjects; ++i)
            {
                IVsBrowseObject browseObj = null;
                browseObj = ppunk[i] as IVsBrowseObject;

                if (browseObj != null)
                {
                    int hr = browseObj.GetProjectItem(out IVsHierarchy hier, out uint itemid);
                    if (hr == VSConstants.S_OK && itemid == VSConstants.VSITEMID_ROOT)
                    {
                        UnconfiguredProject = GetUnconfiguredProject(hier);

                        // We need to save ThreadHandling because the appdesigner will call SetObjects with null, and then call
                        // Deactivate(). We need to run Async code during Deactivate() which requires ThreadHandling.

                        IUnconfiguredProjectVsServices projectVsServices = UnconfiguredProject.Services.ExportProvider.GetExportedValue<IUnconfiguredProjectVsServices>();
                        _threadHandling = projectVsServices.ThreadingService;
                    }
                }
            }

            OnSetObjects(false);
        }
    }
}