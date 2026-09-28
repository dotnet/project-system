// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate;

/// <summary>
/// A context for the up-to-date check with the lifetime of the solution build, which
/// may span multiple project builds.
/// </summary>
internal sealed class SolutionBuildContext
{
    /// <summary>
    /// A cache of timestamps for absolute paths across all projects.
    /// </summary>
    /// <remarks>
    /// In large solutions, many projects check the same inputs and copy items. This cache lasts
    /// for the duration of the solution build. As project builds complete, we clear each
    /// project's output items so that future checks query potentially modified files again.
    /// </remarks>
    public ITimestampCache Timestamps { get; }

    public SolutionBuildContext(IFileSystem fileSystem)
    {
        Timestamps = new ConcurrentTimestampCache(fileSystem);
    }
}
