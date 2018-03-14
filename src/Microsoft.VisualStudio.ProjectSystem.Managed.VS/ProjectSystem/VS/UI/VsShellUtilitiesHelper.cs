// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.VS.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    /// <summary>
    /// Wrapper for VsShellUtilities to allow for testing.
    /// </summary>
    [Export(typeof(IVsShellUtilitiesHelper))]
    internal class VsShellUtilitiesHelper : IVsShellUtilitiesHelper
    {
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public VsShellUtilitiesHelper(IProjectThreadingService threadingService)
        {
            _threadingService = threadingService;
        }

        /// <summary>
        /// <see cref="IVsShellUtilitiesHelper.GetVSVersionAsync"/>
        /// </summary>
        public async Task<Version> GetVSVersionAsync(IServiceProvider serviceProvider)
        {
            await _threadingService.SwitchToUIThread();

            IVsAppId vsAppId = serviceProvider.GetService<IVsAppId, SVsAppId>();
            if (ErrorHandler.Succeeded(vsAppId.GetProperty((int)VSAPropID.VSAPROPID_ProductSemanticVersion, out object oVersion)) &&
                oVersion is string semVersion)
            {
                // This is a semantic version string. We only care about the non-semantic version part
                int index = semVersion.IndexOfAny(new char[] { '-', '+' });
                if (index != -1)
                {
                    semVersion = semVersion.Substring(0, index);
                }

                if (Version.TryParse(semVersion, out Version vsVersion))
                {
                    return vsVersion;
                }
            }

            return null;
        }

        public async Task<string> GetLocalAppDataFolderAsync(IServiceProvider serviceProvider)
        {
            await _threadingService.SwitchToUIThread();

            var shell = serviceProvider.GetService<IVsShell, SVsShell>();
            if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID4.VSSPROPID_LocalAppDataDir, out object objDataFolder)) && objDataFolder is string appDataFolder)
            {
                return appDataFolder;
            }

            return null;
        }
    }
}
