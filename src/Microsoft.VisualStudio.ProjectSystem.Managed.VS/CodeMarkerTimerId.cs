// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable CA1200 // Avoid using cref tags with a prefix

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Represents timer IDs that are passed to <see cref="T:Microsoft.Internal.Performance.CodeMarker"/>.
    /// </summary>
    internal static class CodeMarkerTimerId
    {
        /// <summary>
        ///     Indicates that NuGet package restore has finished.
        /// </summary>
        public const int PerfPackageRestoreEnd = 7343;
    }
}
