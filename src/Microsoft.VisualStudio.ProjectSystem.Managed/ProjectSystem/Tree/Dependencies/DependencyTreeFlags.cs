// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
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
        internal static readonly ProjectTreeFlags DependenciesRootNode = ProjectTreeFlags.Create("DependenciesRootNode");

        /// <summary>
        /// Applied to all top-level dependency items under the dependencies tree.
        /// </summary>
        internal static readonly ProjectTreeFlags Dependency = ProjectTreeFlags.Create("Dependency");

        /// <summary>
        /// Added to dependency tree items which can be removed from the project.
        /// </summary>
        /// <remarks>
        /// Non-<see cref="IDependencyModel.Implicit"/> dependencies are considered removable.
        /// </remarks>
        public static readonly ProjectTreeFlags SupportsRemove = ProjectTreeFlags.Create("SupportsRemove");

        /// <summary>
        /// Indicates that the dependency supports "Open Containing Folder" and "Copy Full Path" commands.
        /// </summary>
        internal static readonly ProjectTreeFlags SupportsBrowse = ProjectTreeFlags.Create(nameof(SupportsBrowse));

        /// <summary>
        /// Indicates that the dependency supports "Open Folder in File Explorer", "Open Containing Folder" and "Copy Full Path" commands.
        /// </summary>
        internal static readonly ProjectTreeFlags SupportsFolderBrowse = ProjectTreeFlags.Create(nameof(SupportsFolderBrowse)) + SupportsBrowse;

        /// <summary>
        /// Dependencies having this flag support displaying a browse object, where the corresponding <see cref="IRule" />
        /// is obtained by <see cref="IDependenciesTreeServices.GetBrowseObjectRuleAsync(IDependency, TargetFramework, IProjectCatalogSnapshot)" />.
        /// </summary>
        internal static readonly ProjectTreeFlags SupportsRuleProperties = ProjectTreeFlags.Create("SupportsRuleProperties");

        /// <summary>
        /// If a dependency is not visible and has this flag, then an empty group node may be displayed for the dependency's provider type.
        /// </summary>
        public static readonly ProjectTreeFlags ShowEmptyProviderRootNode = ProjectTreeFlags.Create("ShowEmptyProviderRootNode");

        /// <summary>
        /// These public flags below are to be used with all nodes: default project item
        /// nodes and all custom nodes provided by third party <see cref="IProjectDependenciesSubTreeProvider"/>
        /// implementations. This is to have a way to distinguish dependency nodes in general.
        /// </summary>
        public static readonly ProjectTreeFlags DependencyFlags
                = Dependency
                + ProjectTreeFlags.Reference
                + SupportsRuleProperties
                + SupportsRemove;

        /// <summary>
        /// The set of flags to assign to unresolved Reference nodes.
        /// </summary>
        public static readonly ProjectTreeFlags UnresolvedDependencyFlags = ProjectTreeFlags.BrokenReference + DependencyFlags;
        
        /// <summary>
        /// The set of flags to assign to resolved Reference nodes.
        /// </summary>
        public static readonly ProjectTreeFlags ResolvedDependencyFlags = ProjectTreeFlags.ResolvedReference + DependencyFlags;

        /// <summary>
        /// Identifies nodes used to group dependencies specific to a given implicit configuration,
        /// which is most commonly the target framework.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Nodes with this flag must also have a flag of form <c>$TFM:FOO</c> where <c>FOO</c> is
        /// the target framework's full moniker.
        /// </para>
        /// <para>
        /// This flag is used by Roslyn when attaching analyzer nodes.
        /// </para>
        /// </remarks>
        internal static readonly ProjectTreeFlags TargetNode = ProjectTreeFlags.Create("TargetNode");

        /// <summary>
        /// Present on nodes that group dependencies of a given provider (e.g. "Packages", "Assemblies", ...).
        /// </summary>
        internal static readonly ProjectTreeFlags DependencyGroup = ProjectTreeFlags.Create("DependencyGroup");

        internal static readonly ProjectTreeFlags AnalyzerDependencyGroup = ProjectTreeFlags.Create("AnalyzerDependencyGroup");
        internal static readonly ProjectTreeFlags AnalyzerDependency = ProjectTreeFlags.Create("AnalyzerDependency");

        internal static readonly ProjectTreeFlags AssemblyDependencyGroup = ProjectTreeFlags.Create("AssemblyDependencyGroup");
        internal static readonly ProjectTreeFlags AssemblyDependency = ProjectTreeFlags.Create("AssemblyDependency");

        internal static readonly ProjectTreeFlags ComDependencyGroup = ProjectTreeFlags.Create("ComDependencyGroup");
        internal static readonly ProjectTreeFlags ComDependency = ProjectTreeFlags.Create("ComDependency");

        internal static readonly ProjectTreeFlags PackageDependencyGroup = ProjectTreeFlags.Create("PackageDependencyGroup");
        internal static readonly ProjectTreeFlags PackageDependency = ProjectTreeFlags.Create("PackageDependency");

        internal static readonly ProjectTreeFlags FrameworkDependencyGroup = ProjectTreeFlags.Create("FrameworkDependencyGroup");
        internal static readonly ProjectTreeFlags FrameworkDependency = ProjectTreeFlags.Create("FrameworkDependency");

        internal static readonly ProjectTreeFlags ProjectDependencyGroup = ProjectTreeFlags.Create("ProjectDependencyGroup");
        internal static readonly ProjectTreeFlags ProjectDependency = ProjectTreeFlags.Create("ProjectDependency");
        internal static readonly ProjectTreeFlags SharedProjectDependency = ProjectTreeFlags.SharedProjectImportReference;

        internal static readonly ProjectTreeFlags SdkDependencyGroup = ProjectTreeFlags.Create("SdkDependencyGroup");
        internal static readonly ProjectTreeFlags SdkDependency = ProjectTreeFlags.Create("SdkDependency");
    }
}
