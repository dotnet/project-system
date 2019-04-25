// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Input
{
    /// <summary>
    /// Provides common well-known command IDs from the WPF flavor.
    /// </summary>
    internal static class WPFCommandId
    {
        // from VS: src\vsproject\fidalgo\WPF\Flavor\WPFFlavor\Guids.cs
        public const long AddWPFWindow = 0x100;
        public const long AddWPFPage = 0x200;
        public const long AddWPFUserControl = 0x300;
        public const long AddWPFResourceDictionary = 0x400;
        public const long WPFWindow = 0x600;
        public const long WPFPage = 0x700;
        public const long WPFUserControl = 0x800;
        public const long WPFResourceDictionary = 0x900;
    }
}
