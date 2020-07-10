// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
