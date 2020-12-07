// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    /// <summary>
    ///     Models presentation information for a node in the dependency tree.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This includes both top-level dependency nodes and grouping nodes
    ///     (e.g. "Packages" or "net5.0"), but not transitive dependencies.
    /// </para>
    /// <para>
    ///     This interface allows representing these different types of tree items
    ///     in a consistent manner.
    /// </para>
    /// </remarks>
    internal interface IDependencyViewModel
    {
        /// <summary>
        /// Friendly name of the dependency, should be used for UI (captions etc)
        /// </summary>
        string Caption { get; }

        /// <summary>
        /// Where appropriate, contains the resolved path of the dependency, otherwise <see langword="null"/>.
        /// </summary>
        string? FilePath { get; }

        /// <summary>
        /// Used in <see cref="IDependenciesTreeServices.GetBrowseObjectRuleAsync"/> to determine the browse
        /// object rule for this dependency.
        /// </summary>
        string? SchemaName { get; }

        /// <summary>
        /// Used in <see cref="IDependenciesTreeServices.GetBrowseObjectRuleAsync"/> to determine the browse
        /// object rule for this dependency.
        /// </summary>
        string? SchemaItemType { get; }

        ImageMoniker Icon { get; }

        ImageMoniker ExpandedIcon { get; }

        ProjectTreeFlags Flags { get; }
    }
}
