// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.CopyPaste
{
    internal static class ClipboardFormat
    {
        /// <summary>
        /// A ushort used by data objects to get and set the text format. Constant defined in WinUser.h.
        /// </summary>
        internal const ushort CF_TEXT = 1;

        /// <summary>
        /// A ushort used by data objects to get and set the unicode text format. Constant defined in WinUser.h.
        /// </summary>
        internal const ushort CF_UNICODETEXT = 13;
    }
}
