// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.FileWatch;

public interface IFileWatcherService : IDisposable
{
    event EventHandler<FileWatcherEventArgs> OnDidCreate;

    event EventHandler<FileWatcherEventArgs> OnDidChange;

    event EventHandler<FileWatcherEventArgs> OnDidDelete;
}

[DataContract]
public class FileWatcherEventArgs : EventArgs
{
    /// <summary>
    /// The Watcher Change Type. It can be: 'created', 'changed' or 'deleted'.
    /// </summary>
    [DataMember(Name = "watcherChangeType")]
    public WatcherChangeTypes WatcherChangeType { get; init; }

    /// <summary>
    /// The URI File System Path
    /// </summary>
    [DataMember(Name = "fsPath")]
    public string? FsPath { get; init; }
}
