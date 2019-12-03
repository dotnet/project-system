// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Interop;

namespace Microsoft.VisualStudio.Shell.Interop
{
    /// <summary>
    /// Wrapper for <see cref="VsShellUtilities"/> to allow for testing.
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

        public async Task<Version?> GetVSVersionAsync(IVsService<IVsAppId> vsAppIdService)
        {
            await _threadingService.SwitchToUIThread();

            IVsAppId? vsAppId = await vsAppIdService.GetValueAsync();

            Assumes.Present(vsAppId);

            if (ErrorHandler.Succeeded(vsAppId.GetProperty((int)VSAPropID.VSAPROPID_ProductSemanticVersion, out object oVersion)) &&
                oVersion is string semVersion)
            {
                // This is a semantic version string. We only care about the non-semantic version part
                int index = semVersion.IndexOfAny(Delimiter.PlusAndMinus);
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

        public async Task<string?> GetLocalAppDataFolderAsync(IVsService<IVsShell> vsShellService)
        {
            await _threadingService.SwitchToUIThread();

            IVsShell? shell = await vsShellService.GetValueAsync();

            Assumes.Present(shell);

            if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID4.VSSPROPID_LocalAppDataDir, out object value)))
            {
                return value as string;
            }

            return null;
        }
    }
}
