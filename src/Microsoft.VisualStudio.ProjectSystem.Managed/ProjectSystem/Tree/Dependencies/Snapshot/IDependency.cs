// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Represents internal immutable dependency entity that is stored in an immutable
    /// <see cref="TargetedDependenciesSnapshot"/>.
    /// </summary>
    internal interface IDependency
    {
        /// <summary>
        /// Gets the set of icons to use for this dependency based on its state (e.g. resolved, expanded).
        /// </summary>
        DependencyIconSet IconSet { get; }

        /// <summary>
        /// Returns a copy of this immutable instance with the specified property changes.
        /// </summary>
        IDependency SetProperties(
            string? caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            string? schemaName = null,
            DependencyIconSet? iconSet = null,
            bool? isImplicit = null);

        #region Copied from IDependencyModel

        /// <summary>
        /// Gets an composite identifier comprised of <see cref="ProviderType"/>
        /// and the originating <see cref="IDependencyModel"/>'s <see cref="IDependencyModel.Id"/>.
        /// </summary>
        /// <remarks>
        /// This string has form <c>"provider-type\model-id"</c>.
        /// See <see cref="Dependency.GetID"/> for details on how this string is constructed.
        /// </remarks>
        string Id { get; }

        /// <summary>
        /// Dependency type, a formal name of the provider type that knows how to create a node
        /// for given dependency.
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// Name of the dependency
        /// </summary>
        string Name { get; }

        /// <summary>
        /// ItemSpec by which dependency could be found in msbuild Project.
        ///     - If dependency is "Resolved" then resolved path will be in Path property,
        ///       and unresolved in OriginalItemSpec.
        ///     - if dependency is "Unresolved" then Path and OriginalItemSpec are the same.
        ///     - if dependency is "custom", i.e. does not have item in the msbuild project or
        ///       item is not represented by xaml rule, then OriginalItemSpec will be ignored
        ///       and should be empty.
        /// </summary>
        string OriginalItemSpec { get; }

        /// <summary>
        /// When <see cref="Resolved"/> is <see langword="true"/>, this contains the resolved path
        /// of the dependency, otherwise it is equal to <see cref="OriginalItemSpec"/>.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Friendly name of the dependency, should be used for UI (captions etc)
        /// </summary>
        string Caption { get; }

        /// <summary>
        /// Used in <see cref="IDependenciesTreeServices.GetBrowseObjectRuleAsync"/> to determine the browse
        /// object rule for this dependency.
        /// </summary>
        string SchemaName { get; }

        /// <summary>
        /// Used in <see cref="IDependenciesTreeServices.GetBrowseObjectRuleAsync"/> to determine the browse
        /// object rule for this dependency.
        /// </summary>
        string SchemaItemType { get; }

        /// <summary>
        /// Used in <see cref="IDependenciesTreeServices.GetBrowseObjectRuleAsync"/> to populate the browse
        /// object for resolved dependencies, to be displayed in property pages (in BrowsableObject context).
        /// </summary>
        IImmutableDictionary<string, string> BrowseObjectProperties { get; }

        /// <summary>
        /// Specifies if dependency is resolved or not
        /// </summary>
        bool Resolved { get; }

        /// <summary>
        /// Specifies if dependency was brought by default and can not be removed/modified by user.
        /// </summary>
        bool Implicit { get; }

        /// <summary>
        /// In some cases dependency should be present in snapshot, but not displayed in the Tree.
        /// </summary>
        bool Visible { get; }

        ProjectTreeFlags Flags { get; }

        #endregion
    }
}
