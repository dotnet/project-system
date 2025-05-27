// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.FileWatch;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.IO;

/// <summary>
/// Simple wrapper around the FileSystemWatcher.
/// </summary>
[Order(Order.Lowest)]
internal sealed class SimpleFileWatcher
{
    private readonly FileSystemWatcher? _fileWatcher;
    public event EventHandler<FileWatcherEventArgs> OnDidCreate;
    public event EventHandler<FileWatcherEventArgs> OnDidChange;
    public event EventHandler<FileWatcherEventArgs> OnDidDelete;

    public SimpleFileWatcher()
    {
    }

    public SimpleFileWatcher(
        string dirToWatch,
        bool includeSubDirs,
        NotifyFilters notifyFilters,
        string fileFilter)
    {
        _fileWatcher = new FileSystemWatcher(dirToWatch)
        {
            IncludeSubdirectories = includeSubDirs,
            NotifyFilter = notifyFilters,
            Filter = fileFilter
        };

        _fileWatcher.Changed += _fileWatcher_handler;
        _fileWatcher.Created += _fileWatcher_handler;
        _fileWatcher.Deleted += _fileWatcher_handler;
        _fileWatcher.Renamed += _fileWatcher_handler;
        _fileWatcher.EnableRaisingEvents = true;
    }

    private void _fileWatcher_handler(object sender, FileSystemEventArgs e)
    {
        var fileWatcherArgs = new FileWatcherEventArgs
        {
            WatcherChangeType = e.ChangeType,
            FsPath = e.FullPath,
        };

        switch (e.ChangeType)
        {
            case WatcherChangeTypes.Created:
                OnDidCreate(this, fileWatcherArgs);
                break;
            case WatcherChangeTypes.Deleted:
                OnDidDelete(this, fileWatcherArgs);
                break;
            case WatcherChangeTypes.Changed:
                OnDidChange(this, fileWatcherArgs);
                break;
            default:
                break;
        }
    }

    public void Dispose()
    {
        if (_fileWatcher is not null)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Dispose();
        }
    }
}

