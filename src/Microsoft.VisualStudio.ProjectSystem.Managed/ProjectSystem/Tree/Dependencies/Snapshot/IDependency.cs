// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Represents internal immutable dependency entity that is stored in an immutable
    /// <see cref="TargetedDependenciesSnapshot"/>.
    /// </summary>
    internal interface IDependency : IDependencyViewModel
    {
        /// <summary>
        /// Gets the set of icons to use for this dependency based on its state (e.g. resolved, expanded).
        /// </summary>
        DependencyIconSet IconSet { get; }

        /// <summary>
        /// Returns a copy of this immutable instance with the specified caption.
        /// </summary>
        IDependency WithCaption(string caption);

        /// <summary>
        /// Gets the originating <see cref="IDependencyModel"/>'s <see cref="IDependencyModel.Id"/>.
        /// </summary>
        /// <remarks>
        /// When combined with <see cref="ProviderType"/> a unique key is obtained for the dependency
        /// within a given target.
        /// </remarks>
        string Id { get; }

        /// <summary>
        /// Dependency type, a formal name of the provider type that knows how to create a node
        /// for given dependency.
        /// </summary>
        /// <remarks>
        /// When combined with <see cref="Id"/> a unique key is obtained for the dependency
        /// within a given target.
        /// </remarks>
        string ProviderType { get; }

        /// <summary>
        /// ItemSpec by which dependency could be found in msbuild Project.
        /// </summary>
        /// <remarks>
        /// Only applies to dependencies modeled in MSBuild project files.
        /// Where non applicable, this property should return <see langword="null"/>.
        /// </remarks>
        string? OriginalItemSpec { get; }

        /// <summary>
        /// Used in <see cref="IDependenciesTreeServices.GetBrowseObjectRuleAsync"/> to populate the browse
        /// object for resolved dependencies, to be displayed in property pages (in BrowsableObject context).
        /// </summary>
        IImmutableDictionary<string, string> BrowseObjectProperties { get; }

        /// <summary>
        /// Gets the level of any diagnostic associated with this dependency (e.g. <see cref="DiagnosticLevel.Error"/>,
        /// <see cref="DiagnosticLevel.Warning"/> and <see cref="DiagnosticLevel.None"/>).
        /// </summary>
        DiagnosticLevel DiagnosticLevel { get; }

        /// <summary>
        /// Gets whether the dependency is resolved or not.
        /// </summary>
        bool Resolved { get; }

        /// <summary>
        /// Gets whether this dependency is declared in an imported project file.
        /// </summary>
        /// <remarks>
        /// If <see langword="true"/>, the dependency was declared outside of the main project file.
        /// It cannot be removed from the project, and should be displayed with an overlay on its
        /// icon.
        /// </remarks>
        bool Implicit { get; }

        /// <summary>
        /// Gets whether this dependency is visible in the tree.
        /// </summary>
        bool Visible { get; }
    }
}
