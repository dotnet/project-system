// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
