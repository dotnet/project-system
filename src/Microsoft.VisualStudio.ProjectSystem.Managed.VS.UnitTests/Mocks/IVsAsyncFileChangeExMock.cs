// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Shell
{
    public class IVsAsyncFileChangeExMock : IVsAsyncFileChangeEx
    {
        private uint _lastCookie;
        private readonly Dictionary<uint, string> _watchedFiles = new Dictionary<uint, string>();
        private readonly HashSet<string> _uniqueFilesWatched = new HashSet<string>();

        public IEnumerable<string> UniqueFilesWatched => _uniqueFilesWatched;

        public IEnumerable<string> WatchedFiles => _watchedFiles.Values;

        public Task<uint> AdviseFileChangeAsync(string filename, _VSFILECHANGEFLAGS filter, IVsFreeThreadedFileChangeEvents2 sink, CancellationToken cancellationToken = default)
        {
            _uniqueFilesWatched.Add(filename);

            uint cookie = _lastCookie++;
            _watchedFiles.Add(cookie, filename);
            return System.Threading.Tasks.Task.FromResult(cookie);
        }

        public Task<string> UnadviseFileChangeAsync(uint cookie, CancellationToken cancellationToken = default)
        {
            string file = _watchedFiles[cookie];
            _watchedFiles.Remove(cookie);
            return System.Threading.Tasks.Task.FromResult(file);
        }

        public Task<uint> AdviseDirChangeAsync(string directory, bool watchSubdirectories, IVsFreeThreadedFileChangeEvents2 sink, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task IgnoreDirAsync(string directory, bool ignore, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task IgnoreFileAsync(uint cookie, string filename, bool ignore, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task SyncFileAsync(string filename, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> UnadviseDirChangeAsync(uint cookie, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
