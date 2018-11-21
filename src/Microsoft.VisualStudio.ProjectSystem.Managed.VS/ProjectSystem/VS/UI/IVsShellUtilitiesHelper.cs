// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.VS.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    internal interface IVsShellUtilitiesHelper
    {
        /// <summary>
        /// Returns the version of VS as defined by VSVSAPROPID_ProductSemanticVersion with the trailing sem version stripped, or null on failure. 
        /// </summary>
        Task<Version> GetVSVersionAsync(IVsService<IVsAppId> vsAppIdService);

        Task<string> GetLocalAppDataFolderAsync(IVsService<IVsShell> vsShellService);
    }
}
