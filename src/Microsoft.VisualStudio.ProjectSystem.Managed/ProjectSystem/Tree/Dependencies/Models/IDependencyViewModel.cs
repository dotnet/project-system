// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    /// <summary>
    ///     Models presentation information for a node in the dependency tree.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This includes both top-level dependency nodes and grouping nodes
    ///     (e.g. "Packages" or ".NET Framework 4.8"), but not transitive dependencies.
    /// </para>
    /// <para>
    ///     This interface allows representing these different types of tree items
    ///     in a consistent manner.
    /// </para>
    /// </remarks>
    internal interface IDependencyViewModel
    {
        string Caption { get; }
        string? FilePath { get; }
        string? SchemaName { get; }
        string? SchemaItemType { get; }
        ImageMoniker Icon { get; }
        ImageMoniker ExpandedIcon { get; }
        ProjectTreeFlags Flags { get; }
        IDependency? Dependency { get; }
    }
}
