// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class VsShellExtensions
    {
        /// <summary>
        /// Directly loads a localized string from a VSPackage satellite DLL.
        /// </summary>
        /// <param name="vsShell">The IVsShell implementation</param>
        /// <param name="packageGuid">Unique identifier of the VSPackage whose UI DLL contains the string specified to load.</param>
        /// <param name="resourceId">Identifier of the string table resource.</param>
        /// <returns>The requested string</returns>
        public static string LoadPackageString(this IVsShell vsShell, Guid packageGuid, uint resourceId)
        {
            ErrorHandler.ThrowOnFailure(vsShell.LoadPackageString(ref packageGuid, resourceId, out string result));

            return result;
        }
    }
}
