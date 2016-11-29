// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal interface IVsShellUtilitiesHelper
    {
        IVsWindowFrame OpenDocument(IServiceProvider serviceProvider, string fullPath);

        void GetRDTDocumentInfo(IServiceProvider serviceProvider,
            string fullPath,
            out IVsHierarchy hierarchy,
            out uint itemid,
            out IVsPersistDocData persistDocData,
            out uint docCookie);
    }

    /// <summary>
    /// Wrapper for VsShellUtilities to allow for testing.
    /// </summary>
    [Export(typeof(IVsShellUtilitiesHelper))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class VsShellUtilitiesHelper : IVsShellUtilitiesHelper
    {
        public void GetRDTDocumentInfo(IServiceProvider serviceProvider, string fullPath, out IVsHierarchy hierarchy, out uint itemid, out IVsPersistDocData persistDocData, out uint docCookie)
        {
            VsShellUtilities.GetRDTDocumentInfo(serviceProvider, fullPath, out hierarchy, out itemid, out persistDocData, out docCookie);
        }

        public IVsWindowFrame OpenDocument(IServiceProvider serviceProvider, string fullPath)
        {
            VsShellUtilities.OpenDocument(serviceProvider, fullPath, Guid.Empty, out IVsUIHierarchy unusedH, out uint unusedI, out IVsWindowFrame frame);
            return frame;
        }
    }
}
