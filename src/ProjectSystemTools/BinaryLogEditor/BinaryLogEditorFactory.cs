// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor
{
    [Guid(ProjectSystemToolsPackage.BinaryLogEditorFactoryGuidString)]
    internal sealed class BinaryLogEditorFactory : IVsEditorFactory
    {
        private static readonly Guid s_logicalViewIdAnyGuid = new Guid(LogicalViewID.Any);
        private static readonly Guid s_logicalViewIdPrimaryGuid = new Guid(LogicalViewID.Primary);
        private static readonly Guid s_logicalViewIdDesignerGuid = new Guid(LogicalViewID.Designer);

        private OLE.Interop.IServiceProvider _site;
        private ServiceProvider _serviceProvider;

        int IVsEditorFactory.CreateEditorInstance(uint vsCreateEditorFlags, string fileName, string physicalView, IVsHierarchy hierarchy, uint itemid, IntPtr existingDocData, out IntPtr docView, out IntPtr docData, out string caption, out Guid commandGuid, out int flags)
        {
            docView = IntPtr.Zero;
            docData = IntPtr.Zero;
            caption = null;
            commandGuid = Guid.Empty;
            flags = 0;

            Cursor oldCursor = Cursor.Current;
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

            return logicalView.Equals(s_logicalViewIdAnyGuid) ||
                   logicalView.Equals(s_logicalViewIdPrimaryGuid) ||
                   logicalView.Equals(s_logicalViewIdDesignerGuid)
                ? VSConstants.S_OK
                : VSConstants.E_NOTIMPL;
        }
    }
}
