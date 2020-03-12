// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Interop
{
    internal static class Win32Interop
    {
        /// <summary>
        ///     Represents the Win32 error code for ERROR_FILE_EXISTS.
        /// </summary>
        public const int ERROR_FILE_EXISTS = 80;

        /// <summary>
        ///     Returns a HRESULT representing the specified Win32 error code.
        /// </summary>
        internal static int HResultFromWin32(int errorCode)
        {
            const int FACILITY_WIN32 = 7;
            uint hr = errorCode <= 0 ? (uint)errorCode : ((uint)(errorCode & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000);
            return (int)hr;
        }
    }
}
