// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
