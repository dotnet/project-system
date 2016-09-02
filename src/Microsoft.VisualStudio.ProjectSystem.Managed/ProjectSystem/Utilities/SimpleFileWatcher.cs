// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// Simple wrapper around the FilesystemWatcher.
    /// </summary>
    internal class SimpleFileWatcher : IDisposable
    {

        private FileSystemWatcher FileWatcher { get; set; }
        private FileSystemEventHandler _handler;
        private RenamedEventHandler _renameHandler;
        
        // For unit tests
        public SimpleFileWatcher()
        {
        }
        
        public SimpleFileWatcher(string dirToWatch, bool includeSubDirs, NotifyFilters notifyFilters, string fileFilter, 
                                       FileSystemEventHandler handler, RenamedEventHandler renameHandler)
        {               
            FileWatcher = new FileSystemWatcher(dirToWatch);
            FileWatcher.IncludeSubdirectories = includeSubDirs;
            FileWatcher.NotifyFilter = notifyFilters;
            FileWatcher.Filter = fileFilter;

            if(handler != null)
            {
                FileWatcher.Created += handler;
                FileWatcher.Deleted += handler;
                FileWatcher.Changed += handler;
            }

            if(renameHandler != null)
            {
                FileWatcher.Renamed += renameHandler;
            }
            FileWatcher.EnableRaisingEvents = true;
            _handler = handler;
            _renameHandler = renameHandler;
        }

        /// <summary>
        /// Cleans up our watcher on the Project.Json file
        /// </summary>
        public void Dispose()
        {               
            if(FileWatcher != null)
            {
                FileWatcher.EnableRaisingEvents = false;
                if(_handler != null)
                {
                    FileWatcher.Created += _handler;
                    FileWatcher.Deleted += _handler;
                    FileWatcher.Changed += _handler;
                    _handler = null;
                }
                if(_renameHandler != null)
                {
                    FileWatcher.Renamed += _renameHandler;
                    _renameHandler = null;
                }
                FileWatcher.Dispose();
                FileWatcher = null;
            }
        }
    }
}

