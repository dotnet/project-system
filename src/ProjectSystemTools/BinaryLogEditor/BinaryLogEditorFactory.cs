// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor
{
    [Guid(ProjectSystemToolsPackage.BinaryLogEditorFactoryGuidString)]
    public sealed class BinaryLogEditorFactory : IVsEditorFactory
    {
        private static readonly Guid LogicalViewIdAnyGuid = new Guid(LogicalViewID.Any);
        private static readonly Guid LogicalViewIdPrimaryGuid = new Guid(LogicalViewID.Primary);
        private static readonly Guid LogicalViewIdDesignerGuid = new Guid(LogicalViewID.Designer);

        private OLE.Interop.IServiceProvider _site;
        private ServiceProvider _serviceProvider;

        int IVsEditorFactory.CreateEditorInstance(uint vsCreateEditorFlags, string fileName, string physicalView, IVsHierarchy hierarchy, uint itemid, IntPtr existingDocData, out IntPtr docView, out IntPtr docData, out string caption, out Guid commandGuid, out int flags)
        {
            docView = IntPtr.Zero;
            docData = IntPtr.Zero;
            caption = null;
            commandGuid = Guid.Empty;
            flags = 0;

            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                if ((vsCreateEditorFlags & (uint)(__VSCREATEEDITORFLAGS.CEF_OPENFILE | __VSCREATEEDITORFLAGS.CEF_SILENT)) == 0)
                {
                    throw new ArgumentException(BinaryLogEditorResources.BadCreateFlags, nameof(vsCreateEditorFlags));
                }

                if (existingDocData != IntPtr.Zero)
                {
                    return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
                }

                caption = string.Empty;
                var documentData = new BinaryLogDocumentData();
                docData = Marshal.GetIUnknownForObject(documentData);
                docView = Marshal.GetIUnknownForObject(new BinaryLogEditorPane(documentData));
                commandGuid = ProjectSystemToolsPackage.BinaryLogEditorUIContextGuid;
            }
            finally
            {
                Cursor.Current = oldCursor;
            }

            return VSConstants.S_OK;
        }

        int IVsEditorFactory.SetSite(OLE.Interop.IServiceProvider site)
        {
            _site = site;
            _serviceProvider = new ServiceProvider(_site, false);
            return VSConstants.S_OK;
        }

        int IVsEditorFactory.Close()
        {
            if (_serviceProvider != null)
            {
                _serviceProvider.Dispose();
                _serviceProvider = null;
            }
            _site = null;
            return VSConstants.S_OK;
        }

        int IVsEditorFactory.MapLogicalView(ref Guid logicalView, out string physicalView)
        {
            physicalView = null;

            return logicalView.Equals(LogicalViewIdAnyGuid) ||
                   logicalView.Equals(LogicalViewIdPrimaryGuid) ||
                   logicalView.Equals(LogicalViewIdDesignerGuid)
                ? VSConstants.S_OK
                : VSConstants.E_NOTIMPL;
        }
    }
}