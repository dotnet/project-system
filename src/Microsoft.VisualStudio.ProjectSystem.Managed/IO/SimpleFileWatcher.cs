// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

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

            if (handler != null)
            {
                _fileWatcher.Created += handler;
                _fileWatcher.Deleted += handler;
                _fileWatcher.Changed += handler;
            }

            if (renameHandler != null)
            {
                _fileWatcher.Renamed += renameHandler;
            }

            _fileWatcher.EnableRaisingEvents = true;
            _handler = handler;
            _renameHandler = renameHandler;
        }

        public void Dispose()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                if (_handler != null)
                {
                    _fileWatcher.Created -= _handler;
                    _fileWatcher.Deleted -= _handler;
                    _fileWatcher.Changed -= _handler;
                    _handler = null;
                }
                if (_renameHandler != null)
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

