// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate;

/// <inheritdoc cref="ISolutionBuildContext" />
[Export(typeof(ISolutionBuildEventListener))]
[Export(typeof(ISolutionBuildContext))]
internal sealed class SolutionBuildContext : ISolutionBuildEventListener, ISolutionBuildContext
{
    private readonly IFileSystem _fileSystem;

    public ITimestampCache? CopyItemTimestamps { get; private set; }

    [ImportingConstructor]
    public SolutionBuildContext(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void NotifySolutionBuildStarting(DateTime buildStartTimeUtc)
    {
        // Initialize a new timestamp cache for both sources and targets of copy items.
        CopyItemTimestamps = new ConcurrentTimestampCache(_fileSystem);
    }

    public void NotifySolutionBuildCompleted()
    {
        // There may be many items in this collection. We don't have to retain them, so free them here.
        CopyItemTimestamps = null;
    }
}
