// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    internal interface IVsShellUtilitiesHelper
    {
        Task<IVsWindowFrame> OpenDocumentWithSpecificEditorAsync(IServiceProvider serviceProvider, string fullPath, Guid editorType, Guid logicalView);

        Task<(IVsHierarchy hierarchy, uint itemid, IVsPersistDocData docData, uint docCookie)> GetRDTDocumentInfoAsync(
            IServiceProvider serviceProvider,
            string fullPath);

        /// <summary>
        /// Returns the version of VS as defined by VSVSAPROPID_ProductSemanticVersion with the trailing sem verson stripped, or null on failure. 
        /// </summary>
        Task<Version> GetVSVersionAsync(IServiceProvider serviceProvider);

        Task<string> GetLocalAppDataFolderAsync(IServiceProvider serviceProvider);

    }
}
