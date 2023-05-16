// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NewFlags = Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.DependencyTreeFlags;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    /// <summary>
    /// Cached immutable instances of <see cref="ProjectTreeFlags"/> used by nodes in the dependencies tree.
    /// </summary>
    /// <remarks>
    /// Members having names ending in <c>Flags</c> are aggregates, containing multiple flags. All others are singular.
    /// </remarks>
    public static class DependencyTreeFlags
    {
        /// <summary>
        /// Identifies the root "Dependencies" node of the dependencies tree. This flag should exist on only one node in a project.
        /// </summary>
        internal static readonly ProjectTreeFlags DependenciesRootNode = ProjectTreeFlags.Create(nameof(DependenciesRootNode));

        /// <summary>
        /// Applied to all top-level dependency items within the dependencies tree.
        /// </summary>
        internal static readonly ProjectTreeFlags Dependency = ProjectTreeFlags.Create(nameof(Dependency));

        /// <summary>
        /// Indicates the user may remove this dependency from the project.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Removal is initiated via the context menu, or by pressing the delete key on the keyboard.
        /// </para>
        /// <para>
        /// For dependencies sourced via MSBuild items, this is typically determined by <c>IsImplicitlyDefined</c> metadata on the item.
        /// </para>
        /// <para>
        /// Removal may also be vetoed by exports of <see cref="IProjectTreeActionHandler"/> with contract name <c>DependencyTreeRemovalActionHandlers</c>.
        /// </para>
        /// </remarks>
        public static readonly ProjectTreeFlags SupportsRemove = ProjectTreeFlags.Create(nameof(SupportsRemove));

        /// <summary>
        /// Indicates that the dependency supports "Open Containing Folder" and "Copy Full Path" commands.
        /// </summary>
        /// <remarks>
        /// Requires the dependency's browse object to contain a <c>BrowsePath</c> value.
        /// </remarks>
        public static readonly ProjectTreeFlags SupportsBrowse = ProjectTreeFlags.Create(nameof(SupportsBrowse));

        /// <summary>
        /// Indicates that the dependency supports "Open Folder in File Explorer", "Open Containing Folder" and "Copy Full Path" commands.
        /// </summary>
        /// <remarks>
        /// Requires the dependency's browse object to contain a <c>BrowsePath</c> value.
        /// </remarks>
        public static readonly ProjectTreeFlags SupportsFolderBrowse = ProjectTreeFlags.Create(nameof(SupportsFolderBrowse)) + SupportsBrowse;

        /// <summary>
        /// Identifies nodes used to group dependencies specific to a given slice, which is most commonly the target framework.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This would be named <c>SliceNode</c> but for the fact that external code has taken a dependency upon this naming.
        /// </para>
        /// <para>
        /// Nodes with this flag must also have a flag of form <c>$TFM:FOO</c> where <c>FOO</c> is the target framework alias.
        /// </para>
        /// <para>
        /// This flag is used by Roslyn when attaching analyzer nodes.
        /// </para>
        /// </remarks>
        internal static readonly ProjectTreeFlags TargetNode = ProjectTreeFlags.Create(nameof(TargetNode));

        /// <summary>
        /// Present on nodes that group dependencies of a given provider (e.g. "Packages", "Assemblies", ...).
        /// </summary>
        internal static readonly ProjectTreeFlags DependencyGroup = ProjectTreeFlags.Create(nameof(DependencyGroup));

        internal static readonly ProjectTreeFlags AnalyzerDependencyGroup = ProjectTreeFlags.Create(nameof(AnalyzerDependencyGroup));
        internal static readonly ProjectTreeFlags AnalyzerDependency = ProjectTreeFlags.Create(nameof(AnalyzerDependency));

        internal static readonly ProjectTreeFlags AssemblyDependencyGroup = ProjectTreeFlags.Create(nameof(AssemblyDependencyGroup));
        internal static readonly ProjectTreeFlags AssemblyDependency = ProjectTreeFlags.Create(nameof(AssemblyDependency));

        internal static readonly ProjectTreeFlags ComDependencyGroup = ProjectTreeFlags.Create(nameof(ComDependencyGroup));
        internal static readonly ProjectTreeFlags ComDependency = ProjectTreeFlags.Create(nameof(ComDependency));

        internal static readonly ProjectTreeFlags PackageDependencyGroup = ProjectTreeFlags.Create(nameof(PackageDependencyGroup));
        internal static readonly ProjectTreeFlags PackageDependency = ProjectTreeFlags.Create(nameof(PackageDependency));

        internal static readonly ProjectTreeFlags FrameworkDependencyGroup = ProjectTreeFlags.Create(nameof(FrameworkDependencyGroup));
        internal static readonly ProjectTreeFlags FrameworkDependency = ProjectTreeFlags.Create(nameof(FrameworkDependency));

        internal static readonly ProjectTreeFlags ProjectDependencyGroup = ProjectTreeFlags.Create(nameof(ProjectDependencyGroup));
        internal static readonly ProjectTreeFlags ProjectDependency = ProjectTreeFlags.Create(nameof(ProjectDependency));
        internal static readonly ProjectTreeFlags SharedProjectDependency = ProjectTreeFlags.SharedProjectImportReference;

        internal static readonly ProjectTreeFlags SdkDependencyGroup = ProjectTreeFlags.Create(nameof(SdkDependencyGroup));
        internal static readonly ProjectTreeFlags SdkDependency = ProjectTreeFlags.Create(nameof(SdkDependency));

        internal static readonly ProjectTreeFlags DependenciesRootNodeFlags
            = DependenciesRootNode
            + ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp)
            + ProjectTreeFlags.Create(ProjectTreeFlags.Common.ReferencesFolder)
            + ProjectTreeFlags.Create(ProjectTreeFlags.Common.VirtualFolder);

        internal static readonly ProjectTreeFlags TargetNodeFlags
            = TargetNode
            + ProjectTreeFlags.Create(ProjectTreeFlags.Common.VirtualFolder);

        /// <summary>
        /// The set of flags to assign to unresolved dependency items within the dependencies tree.
        /// </summary>
        public static readonly ProjectTreeFlags UnresolvedDependencyFlags
            = ProjectTreeFlags.Reference
            + ProjectTreeFlags.BrokenReference
            + Dependency
            + SupportsRemove;

        /// <summary>
        /// The set of flags to assign to resolved dependency items within the dependencies tree.
        /// </summary>
        public static readonly ProjectTreeFlags ResolvedDependencyFlags
            = ProjectTreeFlags.Reference
            + ProjectTreeFlags.ResolvedReference
            + Dependency
            + SupportsRemove;
    }
}

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    // This type used to live in the VS layer, but moved up to the common layer.
    // In an attempt to clean up the namespaces and remove outdated values, this type is marked obsolete.

    /// <summary>
    /// Cached immutable instances of <see cref="ProjectTreeFlags"/> used by nodes in the dependencies tree.
    /// </summary>
    /// <remarks>
    /// Members having names ending in <c>Flags</c> are aggregates, containing multiple flags. All others are singular.
    /// </remarks>
    [Obsolete("Use flags from Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.DependencyTreeFlags instead.")]
    public static class DependencyTreeFlags
    {
        /// <summary>
        /// Added to dependency tree items which can be removed from the project.
        /// </summary>
        /// <remarks>
        /// Non-<see cref="IDependencyModel.Implicit"/> dependencies are considered removable.
        /// </remarks>
        public static readonly ProjectTreeFlags SupportsRemove = NewFlags.SupportsRemove;

        /// <summary>
        /// If a dependency is not visible and has this flag, then an empty group node may be displayed for the dependency's provider type.
        /// </summary>
        [Obsolete("This flag existed when we supported non-visible dependencies, which are no longer supported. If you need this behavior, export a data source that emits an empty ImmutableArray<IDependency> instead.")]
        public static readonly ProjectTreeFlags ShowEmptyProviderRootNode = ProjectTreeFlags.Empty;

        /// <summary>
        /// These public flags below are to be used with all nodes: default project item
        /// nodes and all custom nodes provided by third party <see cref="IProjectDependenciesSubTreeProvider"/>
        /// implementations. This is to have a way to distinguish dependency nodes in general.
        /// </summary>
        [Obsolete("Specify either ResolvedDependencyFlags or UnresolvedDependencyFlags instead.")]
        public static readonly ProjectTreeFlags DependencyFlags = ProjectTreeFlags.Empty;

        /// <summary>
        /// The set of flags to assign to unresolved Reference nodes.
        /// </summary>
        public static readonly ProjectTreeFlags UnresolvedDependencyFlags = NewFlags.UnresolvedDependencyFlags;

        /// <summary>
        /// The set of flags to assign to resolved Reference nodes.
        /// </summary>
        public static readonly ProjectTreeFlags ResolvedDependencyFlags = NewFlags.ResolvedDependencyFlags;
    }
}
