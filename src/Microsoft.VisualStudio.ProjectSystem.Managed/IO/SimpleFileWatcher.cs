// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.IO
{
    /// <summary>
    /// Simple wrapper around the FileSystemWatcher.
    /// </summary>
    internal sealed class SimpleFileWatcher : IDisposable
    {
        private FileSystemWatcher? _fileWatcher;
        private FileSystemEventHandler? _handler;
        private RenamedEventHandler? _renameHandler;

        // For unit tests
        public SimpleFileWatcher()
        {
        }

        public SimpleFileWatcher(string dirToWatch, bool includeSubDirs, NotifyFilters notifyFilters, string fileFilter,
                                 FileSystemEventHandler? handler, RenamedEventHandler? renameHandler)
        {
            _fileWatcher = new FileSystemWatcher(dirToWatch)
            {
                IncludeSubdirectories = includeSubDirs,
                NotifyFilter = notifyFilters,
                Filter = fileFilter
            };

            if (handler is not null)
            {
                _fileWatcher.Created += handler;
                _fileWatcher.Deleted += handler;
                _fileWatcher.Changed += handler;
            }

            if (renameHandler is not null)
            {
                _fileWatcher.Renamed += renameHandler;
            }

            _fileWatcher.EnableRaisingEvents = true;
            _handler = handler;
            _renameHandler = renameHandler;
        }

        public void Dispose()
        {
            if (_fileWatcher is not null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                if (_handler is not null)
                {
                    _fileWatcher.Created -= _handler;
                    _fileWatcher.Deleted -= _handler;
                    _fileWatcher.Changed -= _handler;
                    _handler = null;
                }
                if (_renameHandler is not null)
                {
                    _fileWatcher.Renamed -= _renameHandler;
                    _renameHandler = null;
                }
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }
        }
    }
}

