// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate;

/// <summary>
/// A concurrent-safe timestamp cache.
/// </summary>
/// <remarks>
/// Uses synchronisation to protect the inner data structures from corruption.
///
/// No lock is not held while querying the file system. Concurrent requests for
/// the same file, where that file is not present in the cache, may result in
/// multiple requests to the underlying file system for that file.
/// </remarks>
internal sealed class ConcurrentTimestampCache : ITimestampCache
{
    private readonly Dictionary<string, DateTime?> _timestampCache = new(StringComparers.Paths);

    private readonly IFileSystem _fileSystem;

    public ConcurrentTimestampCache(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public DateTime? GetTimestampUtc(string path)
    {
        lock (_timestampCache)
        {
            if (_timestampCache.TryGetValue(path, out DateTime? existingTime))
            {
                return existingTime;
            }
        }

        // Don't hold the lock while doing the expensive file system query.
        _fileSystem.TryGetLastFileWriteTimeUtc(path, out DateTime? newTime);

        // Re-acquire the lock in order to store the result safely.
        lock (_timestampCache)
        {
            // Note that we will cache a null (file not found) here too.
            _timestampCache[path] = newTime;
        }

        return newTime;
    }

    public void ClearTimestamps(IEnumerable<string> paths)
    {
        lock (_timestampCache)
        {
            foreach (string path in paths)
            {
                _timestampCache.Remove(path);
            }
        }
    }
}
