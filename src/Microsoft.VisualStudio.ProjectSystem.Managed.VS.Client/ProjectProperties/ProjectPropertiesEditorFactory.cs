// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(EditorFactoryGuidString)]
    [ProvideView(LogicalView.Designer, "Design")]
    internal sealed class ProjectPropertiesEditorFactory : IVsEditorFactory
    {
        internal const string EditorFactoryGuidString = "04B8AB82-A572-4FEF-95CE-5222444B6B64";

        // TODO if the new editor is not enabled, fall back to the legacy editor via this GUID
        internal const string LegacyEditorFactoryGuidString = "990036EB-F67A-4B8A-93D4-4663DB2A1033";

        /// <summary>
        /// Logical view identifier (passed to <see cref="MapLogicalView"/>) when the properties
        /// pages are to be launched displaying the "debug" page.
        /// </summary>
        /// <remarks>
        /// Triggered via menu item "Debug | [Project Name] Debug Properties".
        /// </remarks>
        // TODO if we need this value, try and source it from VSHPROPID_ProjectPropertiesDebugPageArg
        internal static readonly Guid DebugPageLogicalViewGuid = new Guid("0273C280-1882-4ED0-9308-52914672E3AA");

        private ProjectPropertiesWindowPaneData? _paneData;

        public int SetSite(IServiceProvider site)
        {
            return HResult.OK;
        }

        public int CreateEditorInstance(
            uint grfCreateDoc,
            string pszMkDocument,
            string pszPhysicalView,
            IVsHierarchy pvHier,
            uint itemid,
            IntPtr punkDocDataExisting,
            out IntPtr ppunkDocView,
            out IntPtr ppunkDocData,
            out string? pbstrEditorCaption,
            out Guid pguidCmdUI,
            out int pgrfCDW)
        {
            // TODO try/catch all this (dispose things created here as needed)

            using var _ = new WaitCursor();

            UIThreadHelper.VerifyOnUIThread();

            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;

            // An empty caption allows the project name to be used as the caption.
            pbstrEditorCaption = "";

            pguidCmdUI = default;
            pgrfCDW = default;

            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0)
            {
                Debug.Fail("Must be opening the file (not cloning it) and creating the editor silently");
                return HResult.InvalidArg;
            }

            _paneData = new ProjectPropertiesWindowPaneData(pvHier, ServiceProvider.GlobalProvider);

            ppunkDocData = Marshal.GetIUnknownForObject(_paneData);

            var newEditor = new ProjectPropertiesWindowPane(pvHier);
            ppunkDocView = Marshal.GetIUnknownForObject(newEditor);

            const _VSRDTFLAGS windowFlags = _VSRDTFLAGS.RDT_DontAddToMRU |    // TODO review this
                                            _VSRDTFLAGS.RDT_CantSave |        // TODO review this -- we do want to save
                                            _VSRDTFLAGS.RDT_ProjSlnDocument |
                                            _VSRDTFLAGS.RDT_VirtualDocument |
                                            _VSRDTFLAGS.RDT_DontAutoOpen;     // TODO review this

            pgrfCDW = (int)windowFlags;

            pbstrEditorCaption = string.Empty; // no need to add anything to the caption
                
            return HResult.OK;
        }

        public int MapLogicalView(ref Guid rguidLogicalView, out string? pbstrPhysicalView)
        {
            // The default physical view for an editor must be null, and we only support one physical view.
            pbstrPhysicalView = null;

            // Disallow TextView, as that suggests the requested view is the XML editor.
            if (rguidLogicalView == VSConstants.LOGVIEWID.TextView_guid)
            {
                return HResult.NotImplemented;
            }

            if (rguidLogicalView == VSConstants.LOGVIEWID.Primary_guid)
            {
                return HResult.OK;
            }

            if (rguidLogicalView == VSConstants.LOGVIEWID.Designer_guid)
            {
                return HResult.OK;
            }

            if (rguidLogicalView == DebugPageLogicalViewGuid)
            {
                // The UI should be launched with the "debug" page selected.
                return HResult.OK;
            }

            // We may also be called with other GUIDs for specific pages in the UI. Rather than list them all here,
            // just return success.

            // TODO determine whether we can handle those GUIDs in the new experience or not and potentially change this to return NotImplemented.
            return HResult.OK;
        }

        public int Close()
        {
            return HResult.OK;
        }

        private sealed class ProjectPropertiesWindowPaneData
        {
            public ProjectPropertiesWindowPaneData(IVsHierarchy vsHierarchy, System.IServiceProvider serviceProvider)
            {
                
            }
        }

        private sealed class ProjectPropertiesWindowPane : WindowPane
        {
            public ProjectPropertiesWindowPane(IVsHierarchy vsHierarchy)
            {
                
            }

            public override object? Content { get; set; }

            protected override void OnCreate()
            {
                UIThreadHelper.VerifyOnUIThread();

                base.OnCreate();

                // TODO provide the actual UI content here
                Content = new TextBlock
                {
                    Text = "Hello world!",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    if (Content != null)
                    {
                        FrameworkElement control = (FrameworkElement)Content;
                        IDisposable? disposable = control.DataContext as IDisposable;
                        disposable?.Dispose();
                    }
                }
            }
        }
    }
}
