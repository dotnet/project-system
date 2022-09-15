// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    public abstract partial class PropertyPage : UserControl, IPropertyPage, IVsDebuggerEvents
    {
        private IPropertyPageSite? _site;
        private bool _isDirty;
        private IVsDebugger? _debugger;
        private uint _debuggerCookie;
        private bool _isActivated;

        // WIN32 Constants
        private const int SW_HIDE = 0;

        protected abstract string PropertyPageName { get; }

        internal PropertyPage()
        {
            AutoScroll = false;
        }

        internal UnconfiguredProject? UnconfiguredProject { get; set; }

        /// <summary>
        /// Property. Gets or sets whether the page is dirty. Dirty status is pushed to owner property sheet
        /// </summary>
        protected bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                // Only process real changes
                if (value != _isDirty)
                {
                    _isDirty = value;

                    // If dirty, this causes Apply to be called
                    _site?.OnStatusChange((uint)(_isDirty ? PROPPAGESTATUS.PROPPAGESTATUS_DIRTY : PROPPAGESTATUS.PROPPAGESTATUS_CLEAN));
                }
            }
        }

        [Obsolete("This property is not used by the project system.")]
        public List<IVsBrowseObjectContext>? ContextObjects => null;

        /// <summary>
        /// IPropertyPage
        /// This is called before our form is shown but after SetObjects is called.
        /// This is the place from which the form can populate itself using the information available
        /// in CPS.
        /// </summary>
        public void Activate(IntPtr hWndParent, RECT[] pRect, int bModal)
        {
            AdviseDebugger();
            SuspendLayout();

            // Initialization can cause some events to be fired when we change some values
            // so we use this flag (_ignoreEvents) to notify IsDirty to ignore
            // any changes that happen during initialization

            Control parent = FromHandle(hWndParent);
            if (parent is not null)
            {   // We're hosted in WinForms, make sure we 
                // set Parent so that we inherit Font & Colors
                Parent = parent;
            }
            else
            {
                Win32Methods.SetParent(Handle, hWndParent);
            }

            ResumeLayout();
            _isActivated = true;
        }

        /// <summary>
        /// This is where the information entered in the form should be saved in CPS
        /// </summary>
        public int Apply()
        {
            Assumes.NotNull(UnconfiguredProject);

            return UnconfiguredProject.Services.ThreadingPolicy.ExecuteSynchronously(OnApplyAsync);
        }

        /// <summary>
        /// Called when the page is deactivated
        /// </summary>
        public void Deactivate()
        {
            if (_isActivated)
            {
                Assumes.NotNull(UnconfiguredProject);

                UnconfiguredProject.Services.ThreadingPolicy.ExecuteSynchronously(OnDeactivateAsync);
                UnadviseDebugger();
            }

            _isActivated = false;
            Dispose(true);
        }

        /// <summary>
        /// Returns a struct describing our property page
        /// </summary>
        public void GetPageInfo(PROPPAGEINFO[] pPageInfo)
        {
            if (pPageInfo?.Length > 0)
            {
                pPageInfo[0] = new PROPPAGEINFO
                {
                    cb = (uint)Marshal.SizeOf(typeof(PROPPAGEINFO)),
                    dwHelpContext = 0,
                    pszDocString = null,
                    pszHelpFile = null,
                    pszTitle = PropertyPageName,
                    // set the size to 0 so the host doesn't use scroll bars
                    // we want to do that within our own container.
                    SIZE = { cx = 0, cy = 0 }
                };
            }
        }

        /// <summary>
        /// Returns the help context
        /// </summary>
        public void Help(string pszHelpDir)
        {
            return;
        }

        public int IsPageDirty()
        {
            if (IsDirty)
                return HResult.OK;

            return HResult.False;
        }

        /// <summary>
        /// IPropertyPage
        ///  Called when the page is moved or sized
        /// </summary>
        public new void Move(RECT[] pRect)
        {
            if (pRect is null || pRect.Length <= 0)
                throw new ArgumentNullException(nameof(pRect));

            RECT r = pRect[0];

            Location = new Point(r.left, r.top);
            Size = new Size(r.right - r.left, r.bottom - r.top);
        }

        /// <summary>
        /// Notification that debug mode changed
        /// </summary>
        public int OnModeChange(DBGMODE dbgmodeNew)
        {
            Enabled = (dbgmodeNew == DBGMODE.DBGMODE_Design);
            return HResult.OK;
        }

        /// <summary>
        /// Informs derived classes that configuration has changed
        /// </summary>
        internal void SetObjects(bool isClosing)
        {
            Assumes.NotNull(UnconfiguredProject);

            UnconfiguredProject.Services.ThreadingPolicy.ExecuteSynchronously(() => OnSetObjectsAsync(isClosing));
        }

        /// <summary>
        /// IPropertyPage
        /// Site for our page
        /// </summary>
        public void SetPageSite(IPropertyPageSite pPageSite)
        {
            _site = pPageSite;
        }

        /// <summary>
        /// IPropertyPage
        /// Show/Hide the page
        /// </summary>
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

        /// <summary>
        /// IPropertyPage
        /// Handles mnemonics
        /// </summary>
        public int TranslateAccelerator(MSG[] pMsg)
        {
            if (pMsg is null)
                return VSConstants.E_POINTER;

            var m = Message.Create(pMsg[0].hwnd, (int)pMsg[0].message, pMsg[0].wParam, pMsg[0].lParam);
            bool used = false;

            // Preprocessing should be passed to the control whose handle the message refers to.
            Control target = FromChildHandle(m.HWnd);
            if (target is not null)
                used = target.PreProcessMessage(ref m);

            if (used)
            {
                pMsg[0].message = (uint)m.Msg;
                pMsg[0].wParam = m.WParam;
                pMsg[0].lParam = m.LParam;
                // Returning S_OK (0) indicates we handled the message ourselves
                return HResult.OK;
            }

            // Returning S_FALSE (1) indicates we have not handled the message
            int result = 0;
            if (_site is not null)
                result = _site.TranslateAccelerator(pMsg);
            return result;
        }

        /// <summary>
        /// Initialize and listen to debug mode changes
        /// </summary>
        internal void AdviseDebugger()
        {
            if (_site is System.IServiceProvider sp)
            {
#pragma warning disable RS0030 // Do not used banned APIs
                _debugger = sp.GetService<IVsDebugger, IVsDebugger>();
#pragma warning restore RS0030 // Do not used banned APIs
                if (_debugger is not null)
                {
                    _debugger.AdviseDebuggerEvents(this, out _debuggerCookie);
                    var dbgMode = new DBGMODE[1];
                    _debugger.GetMode(dbgMode);
                    ((IVsDebuggerEvents)this).OnModeChange(dbgMode[0]);
                }
            }
        }

        /// <summary>
        /// Quit listening to debug mode changes
        /// </summary>
        private void UnadviseDebugger()
        {
            if (_debuggerCookie != 0)
            {
                _debugger?.UnadviseDebuggerEvents(_debuggerCookie);
            }
            _debugger = null;
            _debuggerCookie = 0;
        }

        protected abstract Task<int> OnApplyAsync();
        protected abstract Task OnDeactivateAsync();
        protected abstract Task OnSetObjectsAsync(bool isClosing);

        public void SetObjects(uint cObjects, object[] ppunk)
        {
            if (cObjects == 0)
            {
                // If we have never configured anything (maybe a failure occurred on open so app designer is closing us). In this case
                // do nothing
                if (UnconfiguredProject is not null)
                {
                    SetObjects(isClosing: true);
                    UnconfiguredProject = null;
                }

                return;
            }

            UnconfiguredProject = null;

            if (ppunk.Length < cObjects)
                throw new ArgumentOutOfRangeException(nameof(cObjects));

            // Look for an IVsBrowseObject
            for (int i = 0; i < cObjects; ++i)
            {
                if (ppunk[i] is IVsBrowseObject browseObj)
                {
                    HResult hr = browseObj.GetProjectItem(out IVsHierarchy hier, out uint itemid);

                    if (hr.IsOK && itemid == VSConstants.VSITEMID_ROOT)
                    {
                        UnconfiguredProject = hier.AsUnconfiguredProject();
                    }
                }
            }

            _ = OnSetObjectsAsync(isClosing: false);
        }
    }
}
