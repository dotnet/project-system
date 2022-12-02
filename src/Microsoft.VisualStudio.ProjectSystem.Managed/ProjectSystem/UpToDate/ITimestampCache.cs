// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate;

/// <summary>
/// Defines a cache of timestamps for files.
/// </summary>
internal interface ITimestampCache
{
    /// <summary>
    /// Lazily gets the last write time of the specified file.
    /// If the value already exists in this cache, return the cached value.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The last time this file was modified in UTC, or <see langword="null"/> if the file does not exist.</returns>
    DateTime? GetTimestampUtc(string path);

    /// <summary>
    /// Clears any cached timestamps for all <paramref name="paths"/>.
    /// </summary>
    /// <param name="paths">Paths of files to remove from this cache.</param>
    void ClearTimestamps(IEnumerable<string> paths);
}
