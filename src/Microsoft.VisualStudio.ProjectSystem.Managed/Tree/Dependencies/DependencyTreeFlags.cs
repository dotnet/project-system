// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
        internal static readonly ProjectTreeFlags DependenciesRootNodeFlags
                = ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp
                                          | ProjectTreeFlags.Common.ReferencesFolder
                                          | ProjectTreeFlags.Common.VirtualFolder)
                                  .Add("DependenciesRootNode");
        /// <summary>
        /// The set of flags common to all Reference nodes.
        /// </summary>
        public static readonly ProjectTreeFlags BaseReferenceFlags
                = ProjectTreeFlags.Create(ProjectTreeFlags.Common.Reference);

        /// <summary>
        /// The set of flags to assign to unresolved Reference nodes.
        /// </summary>
        /// <remarks>
        /// Contains <see cref="ProjectTreeFlags.Common.BrokenReference"/> which stops
        /// <c>IGraphProvider</c> APIs from being called for that node.
        /// </remarks>
        internal static readonly ProjectTreeFlags UnresolvedReferenceFlags
                = BaseReferenceFlags.Add(ProjectTreeFlags.Common.BrokenReference);

        /// <summary>
        /// The set of flags to assign to resolved Reference nodes.
        /// </summary>
        internal static readonly ProjectTreeFlags ResolvedReferenceFlags
                = BaseReferenceFlags.Add(ProjectTreeFlags.Common.ResolvedReference);

        internal static readonly ProjectTreeFlags GenericDependency = ProjectTreeFlags.Create("GenericDependency");

        public static readonly ProjectTreeFlags SupportsRemove = ProjectTreeFlags.Create("SupportsRemove");

        internal static readonly ProjectTreeFlags SupportsRuleProperties = ProjectTreeFlags.Create("SupportsRuleProperties");

        /// <summary>
        /// This flag indicates that dependency can show a hierarchy of dependencies
        /// </summary>
        public static readonly ProjectTreeFlags SupportsHierarchy = ProjectTreeFlags.Create("SupportsHierarchy");

        public static readonly ProjectTreeFlags ShowEmptyProviderRootNode = ProjectTreeFlags.Create("ShowEmptyProviderRootNode");

        /// <summary>
        /// These public flags below are to be used with all nodes: default project item
        /// nodes and all custom nodes provided by third party <see cref="IProjectDependenciesSubTreeProvider"/>
        /// implementations. This is to have a way to distinguish dependency nodes in general.
        /// </summary>
        public static readonly ProjectTreeFlags DependencyFlags
                = ProjectTreeFlags.Create("Dependency")
                                  .Add(ProjectTreeFlags.Common.VirtualFolder.ToString())
                                  .Add(ProjectTreeFlags.Common.BubbleUp)
                                  .Union(SupportsRuleProperties)
                                  .Union(SupportsRemove);

        internal static readonly ProjectTreeFlags Unresolved = ProjectTreeFlags.Create("Unresolved");
        internal static readonly ProjectTreeFlags Resolved = ProjectTreeFlags.Create("Resolved");

        public static readonly ProjectTreeFlags UnresolvedDependencyFlags = Unresolved.Union(DependencyFlags);
        public static readonly ProjectTreeFlags ResolvedDependencyFlags = Resolved.Union(DependencyFlags);

        internal static readonly ProjectTreeFlags GenericUnresolvedDependencyFlags
                = UnresolvedDependencyFlags.Union(UnresolvedReferenceFlags)
                                           .Union(GenericDependency);

        internal static readonly ProjectTreeFlags GenericResolvedDependencyFlags
                = ResolvedDependencyFlags.Union(ResolvedReferenceFlags)
                                         .Union(GenericDependency);

        internal static readonly ProjectTreeFlags TargetNode = ProjectTreeFlags.Create("TargetNode");
        internal static readonly ProjectTreeFlags SubTreeRootNode = ProjectTreeFlags.Create("SubTreeRootNode");

        internal static readonly ProjectTreeFlags AnalyzerSubTreeRootNode = ProjectTreeFlags.Create("AnalyzerSubTreeRootNode");
        internal static readonly ProjectTreeFlags AnalyzerDependency = ProjectTreeFlags.Create("AnalyzerDependency");

        internal static readonly ProjectTreeFlags AssemblySubTreeRootNode = ProjectTreeFlags.Create("AssemblySubTreeRootNode");
        internal static readonly ProjectTreeFlags AssemblyDependency = ProjectTreeFlags.Create("AssemblyDependency");

        internal static readonly ProjectTreeFlags ComSubTreeRootNode = ProjectTreeFlags.Create("ComSubTreeRootNode");
        internal static readonly ProjectTreeFlags ComDependency = ProjectTreeFlags.Create("ComDependency");

        internal static readonly ProjectTreeFlags NuGetSubTreeRootNode = ProjectTreeFlags.Create("NuGetSubTreeRootNode");
        internal static readonly ProjectTreeFlags NuGetDependency = ProjectTreeFlags.Create("NuGetDependency");
        internal static readonly ProjectTreeFlags NuGetPackageDependency = ProjectTreeFlags.Create("NuGetPackageDependency");
        internal static readonly ProjectTreeFlags FrameworkAssembliesNode = ProjectTreeFlags.Create("FrameworkAssembliesNode");
        internal static readonly ProjectTreeFlags FxAssemblyDependency = ProjectTreeFlags.Create("FxAssemblyDependency");

        internal static readonly ProjectTreeFlags ProjectSubTreeRootNode = ProjectTreeFlags.Create("ProjectSubTreeRootNode");
        internal static readonly ProjectTreeFlags ProjectDependency = ProjectTreeFlags.Create("ProjectDependency");

        internal static readonly ProjectTreeFlags SharedProjectFlags
            = ProjectTreeFlags.Create("SharedProjectDependency")
                              .Add(ProjectTreeFlags.Common.SharedProjectImportReference);

        internal static readonly ProjectTreeFlags Diagnostic = ProjectTreeFlags.Create("Diagnostic");
        internal static readonly ProjectTreeFlags ErrorDiagnostic = ProjectTreeFlags.Create("ErrorDiagnostic");
        internal static readonly ProjectTreeFlags WarningDiagnostic = ProjectTreeFlags.Create("WarningDiagnostic");

        internal static readonly ProjectTreeFlags SdkSubTreeRootNode = ProjectTreeFlags.Create("SdkSubTreeRootNode");
        internal static readonly ProjectTreeFlags SdkDependency = ProjectTreeFlags.Create("SdkDependency");
    }
}
