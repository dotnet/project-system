// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Interop;
using Microsoft.VisualStudio.Setup.Configuration;

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

            IVsAppId vsAppId = await vsAppIdService.GetValueAsync();

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

            IVsShell shell = await vsShellService.GetValueAsync();

            if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID4.VSSPROPID_LocalAppDataDir, out object value)))
            {
                return value as string;
            }

            return null;
        }

        public async Task<string?> GetRegistryRootAsync(IVsService<IVsShell> vsShellService)
        {
            await _threadingService.SwitchToUIThread();

            IVsShell shell = await vsShellService.GetValueAsync();

            if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID.VSSPROPID_VirtualRegistryRoot, out object value)))
            {
                return value as string;
            }

            return null;
        }

        public async Task<bool> IsVSFromPreviewChannelAsync()
        {
            await _threadingService.SwitchToUIThread();

            try
            {
                ISetupConfiguration vsSetupConfig = new SetupConfiguration();
                var setupInstance = vsSetupConfig.GetInstanceForCurrentProcess();
                // NOTE: this explicit cast is necessary for the subsequent COM QI to succeed. 
                var setupInstanceCatalog = (ISetupInstanceCatalog)setupInstance;
                return setupInstanceCatalog.IsPrerelease();
            }
            catch (COMException ex)
            {
                TraceUtilities.TraceError("Failed to determine whether setup instance catalog is prerelease: {0}", ex.ToString());
                return false;
            }
        }
    }
}
