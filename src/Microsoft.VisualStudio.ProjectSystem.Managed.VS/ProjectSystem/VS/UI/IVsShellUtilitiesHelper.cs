// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    internal interface IVsShellUtilitiesHelper
    {
        /// <summary>
        /// Returns the version of VS as defined by VSVSAPROPID_ProductSemanticVersion with the trailing sem verson stripped, or null on failure. 
        /// </summary>
        Task<Version> GetVSVersionAsync(IServiceProvider serviceProvider);

        Task<string> GetLocalAppDataFolderAsync(IServiceProvider serviceProvider);

    }
}
