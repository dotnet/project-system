// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    /// <summary>
    /// Wrapper for VsShellUtilities to allow for testing.
    /// </summary>
    [Export(typeof(IVsShellUtilitiesHelper))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class VsShellUtilitiesHelper : IVsShellUtilitiesHelper
    {
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public VsShellUtilitiesHelper(IProjectThreadingService threadingService)
        {
            _threadingService = threadingService;
        }

        public async Task<(IVsHierarchy hierarchy, uint itemid, IVsPersistDocData docData, uint docCookie)> GetRDTDocumentInfoAsync(
            IServiceProvider serviceProvider,
            string fullPath)
        {
            await _threadingService.SwitchToUIThread();
            VsShellUtilities.GetRDTDocumentInfo(serviceProvider, fullPath, out IVsHierarchy hierarchy, out uint itemid, out IVsPersistDocData persistDocData, out uint docCookie);
            return (hierarchy, itemid, persistDocData, docCookie);
        }

        public async Task<IVsWindowFrame> OpenDocumentWithSpecificEditorAsync(IServiceProvider serviceProvider, string fullPath, Guid editorType, Guid logicalView)
        {
            await _threadingService.SwitchToUIThread();
            return VsShellUtilities.OpenDocumentWithSpecificEditor(serviceProvider, fullPath, editorType, logicalView);

        }
    }
}
