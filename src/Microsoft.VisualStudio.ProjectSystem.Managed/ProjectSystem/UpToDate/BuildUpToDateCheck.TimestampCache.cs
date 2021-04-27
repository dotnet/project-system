// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        private readonly struct TimestampCache
        {
            private readonly IDictionary<string, DateTime> _timestampCache;
            private readonly IFileSystem _fileSystem;

            public TimestampCache(IFileSystem fileSystem)
            {
                Requires.NotNull(fileSystem, nameof(fileSystem));

                _fileSystem = fileSystem;
                _timestampCache = new Dictionary<string, DateTime>(StringComparers.Paths);
            }

            /// <summary>
            /// Gets the number of unique files added to this cache.
            /// </summary>
            public int Count => _timestampCache.Count;

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

            public bool TryGetLatestInput(ImmutableArray<string> inputs, [NotNullWhen(returnValue: true)] out string? latestPath, out DateTime latestTime)
            {
                latestTime = DateTime.MinValue;
                latestPath = null;

                foreach (string input in inputs)
                {
                    DateTime? time = GetTimestampUtc(input);

                    if (time > latestTime)
                    {
                        latestTime = time.Value;
                        latestPath = input;
                    }
                }

                return latestPath != null;
            }
        }
    }
}
