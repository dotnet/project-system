// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Microsoft.VisualStudio.Shell.Interop
{
    [Export(typeof(IVsShellUtilitiesHelper))]
    internal class VsShellUtilitiesHelper : IVsShellUtilitiesHelper
    {
        private static readonly Lazy<bool> s_isPreviewChannel = new(() =>
            {
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
            });

        public bool IsVSFromPreviewChannel()
        {
            return s_isPreviewChannel.Value;
        }
    }
}
