// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        internal readonly struct TimestampCache
        {
            private readonly Dictionary<string, DateTime> _timestampCache = new(StringComparers.Paths);
            private readonly IFileSystem _fileSystem;

            public TimestampCache(IFileSystem fileSystem)
            {
                _fileSystem = fileSystem;
            }

            /// <summary>
            /// Gets the number of unique files added to this cache.
            /// </summary>
            public int Count => _timestampCache.Count;

            /// <summary>
            /// Attempts to get the last write time of the specified file.
            /// If the value already exists in this cache, return the cached value.
            /// Otherwise, query the filesystem, cache the result, then return it.
            /// If the file is not found, return <see langword="null"/>.
            /// </summary>
            public DateTime? GetTimestampUtc(string path)
            {
                if (!_timestampCache.TryGetValue(path, out DateTime time))
                {
                    if (!_fileSystem.TryGetLastFileWriteTimeUtc(path, out DateTime? newTime))
                    {
                        return null;
                    }

                    time = newTime.Value;
                    _timestampCache[path] = time;
                }

                return time;
            }
        }
    }
}
