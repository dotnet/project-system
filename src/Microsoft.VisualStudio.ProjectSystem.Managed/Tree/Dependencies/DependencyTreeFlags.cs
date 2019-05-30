// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
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
        /// The set of flags to assign to unresolvable Reference nodes.
        /// Note: when dependency has ProjectTreeFlags.Common.BrokenReference flag, GraphProvider API are not 
        /// called for that node.
        /// </summary>
        internal static readonly ProjectTreeFlags UnresolvedReferenceFlags
                = BaseReferenceFlags.Add(ProjectTreeFlags.Common.BrokenReference);

        /// <summary>
        /// The set of flags to assign to resolved Reference nodes.
        /// </summary>
        internal static readonly ProjectTreeFlags ResolvedReferenceFlags
                = BaseReferenceFlags.Add(ProjectTreeFlags.Common.ResolvedReference);

        internal static readonly ProjectTreeFlags GenericDependencyFlags = ProjectTreeFlags.Create("GenericDependency");

        public static readonly ProjectTreeFlags SupportsRemove = ProjectTreeFlags.Create("SupportsRemove");

        internal static readonly ProjectTreeFlags SupportsRuleProperties = ProjectTreeFlags.Create("SupportsRuleProperties");

        /// <summary>
        /// This flag indicates that dependency can show a hierarchy of dependencies
        /// </summary>
        public static readonly ProjectTreeFlags SupportsHierarchy = ProjectTreeFlags.Create("SupportsHierarchy");

        public static readonly ProjectTreeFlags ShowEmptyProviderRootNode = ProjectTreeFlags.Create("ShowEmptyProviderRootNode");

        /// <summary>
        /// These public flags below are to be used with all nodes: default project item
        /// nodes and all custom nodes provided by third party IProjectDependenciesSubTreeProvider
        /// implementations. This is to have a way to distinguish dependency nodes in general.
        /// </summary>
        public static readonly ProjectTreeFlags DependencyFlags
                = ProjectTreeFlags.Create("Dependency")
                                  .Add(ProjectTreeFlags.Common.VirtualFolder.ToString())
                                  .Add(ProjectTreeFlags.Common.BubbleUp)
                                  .Union(SupportsRuleProperties)
                                  .Union(SupportsRemove);

        internal static readonly ProjectTreeFlags UnresolvedFlags = ProjectTreeFlags.Create("Unresolved");
        internal static readonly ProjectTreeFlags ResolvedFlags = ProjectTreeFlags.Create("Resolved");

        public static readonly ProjectTreeFlags UnresolvedDependencyFlags = UnresolvedFlags.Union(DependencyFlags);
        public static readonly ProjectTreeFlags ResolvedDependencyFlags = ResolvedFlags.Union(DependencyFlags);

        internal static readonly ProjectTreeFlags GenericUnresolvedDependencyFlags
                = UnresolvedDependencyFlags.Union(UnresolvedReferenceFlags)
                                           .Union(GenericDependencyFlags);

        internal static readonly ProjectTreeFlags GenericResolvedDependencyFlags
                = ResolvedDependencyFlags.Union(ResolvedReferenceFlags)
                                         .Union(GenericDependencyFlags);

        internal static readonly ProjectTreeFlags TargetNodeFlags = ProjectTreeFlags.Create("TargetNode");
        internal static readonly ProjectTreeFlags SubTreeRootNodeFlags = ProjectTreeFlags.Create("SubTreeRootNode");

        internal static readonly ProjectTreeFlags AnalyzerSubTreeRootNodeFlags = ProjectTreeFlags.Create("AnalyzerSubTreeRootNode");
        internal static readonly ProjectTreeFlags AnalyzerSubTreeNodeFlags = ProjectTreeFlags.Create("AnalyzerDependency");

        internal static readonly ProjectTreeFlags AssemblySubTreeRootNodeFlags = ProjectTreeFlags.Create("AssemblySubTreeRootNode");
        internal static readonly ProjectTreeFlags AssemblySubTreeNodeFlags = ProjectTreeFlags.Create("AssemblyDependency");

        internal static readonly ProjectTreeFlags ComSubTreeRootNodeFlags = ProjectTreeFlags.Create("ComSubTreeRootNode");
        internal static readonly ProjectTreeFlags ComSubTreeNodeFlags = ProjectTreeFlags.Create("ComDependency");

        internal static readonly ProjectTreeFlags NuGetSubTreeRootNodeFlags = ProjectTreeFlags.Create("NuGetSubTreeRootNode");
        internal static readonly ProjectTreeFlags NuGetSubTreeNodeFlags = ProjectTreeFlags.Create("NuGetDependency");
        internal static readonly ProjectTreeFlags PackageNodeFlags = ProjectTreeFlags.Create("NuGetPackageDependency");
        internal static readonly ProjectTreeFlags FrameworkAssembliesNodeFlags = ProjectTreeFlags.Create("FrameworkAssembliesNode");
        internal static readonly ProjectTreeFlags FxAssemblyProjectFlags = ProjectTreeFlags.Create("FxAssemblyDependency");

        internal static readonly ProjectTreeFlags ProjectSubTreeRootNodeFlags = ProjectTreeFlags.Create("ProjectSubTreeRootNode");
        internal static readonly ProjectTreeFlags ProjectNodeFlags = ProjectTreeFlags.Create("ProjectDependency");
        internal static readonly ProjectTreeFlags SharedProjectFlags
            = ProjectTreeFlags.Create("SharedProjectDependency")
                              .Add(ProjectTreeFlags.Common.SharedProjectImportReference);

        internal static readonly ProjectTreeFlags DiagnosticNodeFlags = ProjectTreeFlags.Create("Diagnostic");
        internal static readonly ProjectTreeFlags DiagnosticErrorNodeFlags = ProjectTreeFlags.Create("ErrorDiagnostic");
        internal static readonly ProjectTreeFlags DiagnosticWarningNodeFlags = ProjectTreeFlags.Create("WarningDiagnostic");

        internal static readonly ProjectTreeFlags SdkSubTreeRootNodeFlags = ProjectTreeFlags.Create("SdkSubTreeRootNode");
        internal static readonly ProjectTreeFlags SdkSubTreeNodeFlags = ProjectTreeFlags.Create("SdkDependency");
    }
}
