// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate;

/// <summary>
/// A context for the up-to-date check with the lifetime of the solution build, which
/// may span multiple project builds.
/// </summary>
internal interface ISolutionBuildContext
{
    /// <summary>
    /// A cache of timestamps for the absolute paths of both source and target of copy items
    /// across all projects.
    /// </summary>
    /// <remarks>
    /// In large solutions we end up checking many, many copy items. These items get their
    /// own cache that lasts the duration of the solution build, rather than the default cache
    /// which only lasts for the project build. This cache has a high hit rate. As project
    /// builds complete, we clear each project's output items from the cache so that we re-query
    /// them on the next call.
    /// </remarks>
    ITimestampCache? CopyItemTimestamps { get; }
}
