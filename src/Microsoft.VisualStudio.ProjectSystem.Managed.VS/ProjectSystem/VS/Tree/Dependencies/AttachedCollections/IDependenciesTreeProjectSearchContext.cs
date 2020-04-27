// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Provides services throughout the lifetime of a search operation.
    /// </summary>
    public interface IDependenciesTreeProjectSearchContext
    {
        /// <summary>
        /// Gets a token that aborts the search operation if cancelled.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the unconfigured project being searched.
        /// </summary>
        UnconfiguredProject UnconfiguredProject { get; }

        /// <summary>
        /// The set of target frameworks exposed by this project.
        /// </summary>
        ImmutableArray<string> TargetFrameworks { get; }

        /// <summary>
        /// Gets a sub-context, specific to a given target framework of the project.
        /// </summary>
        /// <param name="targetFramework">The target framework being searched.</param>
        /// <returns>The sub-context for the target framework, or <see langword="null"/> if not found.</returns>
        IDependenciesTreeProjectTargetSearchContext? ForTarget(string targetFramework);
    }
}
