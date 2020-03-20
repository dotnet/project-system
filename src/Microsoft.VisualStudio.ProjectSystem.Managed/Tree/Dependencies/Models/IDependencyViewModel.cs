// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    /// <summary>
    ///     Models presentation information for a node in the dependency tree.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This includes not just dependencies, but also subtrees (eg. "Packages")
    ///     and graph nodes.
    /// </para>
    /// <para>
    ///     This interface allows representing these different types of tree items
    ///     in a consistent manner for use with graph/tree APIs.
    /// </para>
    /// </remarks>
    internal interface IDependencyViewModel
    {
        string Caption { get; }
        string? FilePath { get; }
        string? SchemaName { get; }
        string? SchemaItemType { get; }
        int Priority { get; }
        ImageMoniker Icon { get; }
        ImageMoniker ExpandedIcon { get; }
        ProjectTreeFlags Flags { get; }
        IDependency? OriginalModel { get; }
    }
}
