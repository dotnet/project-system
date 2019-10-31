// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

            public DateTime? GetTimestampUtc(string path)
            {
                if (!_timestampCache.TryGetValue(path, out DateTime time))
                {
                    if (!_fileSystem.FileExists(path))
                    {
                        return null;
                    }

                    time = _fileSystem.LastFileWriteTimeUtc(path);
                    _timestampCache[path] = time;
                }

                return time;
            }

            public bool TryGetLatestInput(IEnumerable<string> inputs, [NotNullWhen(returnValue: true)] out string? latestPath, out DateTime latestTime)
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
