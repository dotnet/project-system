// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides Projects sub node to global Dependencies project tree node.
    /// </summary>
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class ProjectDependenciesSubTreeProvider : DependenciesSubTreeProviderBase
    {
        public const string ProviderTypeString = "ProjectDependency";

        public readonly ProjectTreeFlags ProjectSubTreeRootNodeFlags 
                    = ProjectTreeFlags.Create("ProjectSubTreeRootNode");

        public readonly ProjectTreeFlags ProjectSubTreeNodeFlags
                    = ProjectTreeFlags.Create("ProjectSubTreeNode");

        [ImportingConstructor]
        public ProjectDependenciesSubTreeProvider(UnconfiguredProject unconfiguredProject,
                                                  IDependenciesGraphProjectContextProvider projectContextProvider)
        {
            UnconfiguredProject = unconfiguredProject;
            ProjectContextProvider = projectContextProvider;

            // subscribe to design time build to get corresponding items
            UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(ProjectReference.SchemaName);
            ResolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(ResolvedProjectReference.SchemaName);
        }

        private UnconfiguredProject UnconfiguredProject { get; }
        private IDependenciesGraphProjectContextProvider ProjectContextProvider { get; set; }

        public override string ProviderType
        {
            get
            {
                return ProviderTypeString;
            }
        }

        /// <summary>
        /// Specifies if dependency sub node thinks that it is in error state. Different sub nodes
        /// can have different conditions for error state.
        /// </summary>
        public override bool IsInErrorState
        {
            get
            {
                return false;
            }
        }

        private List<ImageMoniker> _nodeIcons = new List<ImageMoniker>
        {
            KnownMonikers.Application
        };

        public override IEnumerable<ImageMoniker> Icons
        {
            get
            {
                return _nodeIcons;
            }
        }

        protected override IDependencyNode CreateRootNode()
        {
            return new SubTreeRootDependencyNode(ProviderType, 
                                                 VSResources.ProjectsNodeName,
                                                 ProjectSubTreeRootNodeFlags,
                                                 KnownMonikers.ApplicationGroup);
        }

        protected override IDependencyNode CreateDependencyNode(string itemSpec,
                                                                string itemType,
                                                                int priority = 0,
                                                                IImmutableDictionary<string, string> properties = null,
                                                                bool resolved = true)
        {
            var projectPath = itemSpec;
            if (!Path.IsPathRooted(projectPath))
            {
                var currentProjectFolder = Path.GetDirectoryName(UnconfiguredProject.FullPath);
                projectPath = Path.GetFullPath(Path.Combine(currentProjectFolder, projectPath));
            }

            var id = new DependencyNodeId(ProviderType,
                                          itemSpec,
                                          itemType ?? ResolvedProjectReference.PrimaryDataSourceItemType,
                                          uniqueToken: projectPath);

            return CreateDependencyNode(id, priority, properties, resolved);
        }

        /// <summary>
        /// Create a project dependency node given ID
        /// </summary>
        private IDependencyNode CreateDependencyNode(DependencyNodeId id,
                                                     int priority,
                                                     IImmutableDictionary<string, string> properties,
                                                     bool resolved)
        {
            return new ProjectDependencyNode(id,
                                             flags: ProjectSubTreeNodeFlags,
                                             priority: priority,
                                             properties: properties,
                                             resolved: resolved);
        }

        /// <summary>
        /// Returns refreshed dependnecy node. There is a non trivial logic which is important:
        /// in order to find node at anypoint of time, we always fill live node instance children collection,
        /// which then could be found by recusrive search from RootNode. However we return always a new 
        /// instance for requested node, to allow graph provider to compare old node instances to new ones
        /// when some thing changes in the dependent project and track changes. So in 
        /// DependenciesGrpahProvider.TrackChangesOnGraphContextAsync we always remember old children
        /// collection for existing node, since for top level nodes (sent by CPS IProjectTree(s) we do update
        /// them here, which would make no difference between old and new nodes. For lower hierarchies below top 
        /// nodes, new instances solve this problem.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public override IDependencyNode GetDependencyNode(DependencyNodeId nodeId)
        {
            if (nodeId == null)
            {
                return null;
            }

            // normalize id to have regular paths (graph provider replaces \ with /.
            nodeId = nodeId.ToNormalizedId();
            var node = RootNode.FindNode(nodeId, recursive: true);
            if (node == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(nodeId.UniqueToken))
            {
                return node;
            }

            var projectPath = nodeId.UniqueToken;
            var projectContext = ProjectContextProvider.GetProjectContext(projectPath);
            if (projectContext == null)
            {
                return node;
            }

            node.Children.Clear();
            foreach (var subTreeProvider in projectContext.GetProviders())
            {
                if (subTreeProvider.RootNode == null || !subTreeProvider.RootNode.HasChildren)
                {
                    continue;
                }

                foreach (var child in subTreeProvider.RootNode.Children)
                {
                    node.AddChild(child);
                }
            }

            // create a new instance of the node to allow graph provider to compare changes
            var newNode = CreateDependencyNode(node.Id, node.Priority, node.Properties, node.Resolved);
            newNode.Children.AddRange(node.Children);
            return newNode;
        }

        /// <summary>
        /// Updates the shared project import nodes that are shown under the 'Dependencies/Projects' node.
        /// </summary>
        /// <param name="sharedFolders">Snapshot of shared folders.</param>
        /// <param name="dependenciesChange"></param>
        /// <returns></returns>
        protected override void ProcessSharedProjectImportNodes(IProjectSharedFoldersSnapshot sharedFolders,
                                                                DependenciesChange dependenciesChange)
        {
            Requires.NotNull(sharedFolders, nameof(sharedFolders));
            Requires.NotNull(dependenciesChange, nameof(dependenciesChange));

            var sharedFolderProjectPaths = sharedFolders.Value.Select(sf => sf.ProjectPath);
            var currentSharedImportNodes = RootNode.Children
                    .Where(x => x.Flags.Contains(ProjectTreeFlags.Common.SharedProjectImportReference));
            var currentSharedImportNodePaths = currentSharedImportNodes.Select(x => x.Id.ItemSpec);

            // process added nodes
            IEnumerable<string> addedSharedImportPaths = sharedFolderProjectPaths.Except(currentSharedImportNodePaths);
            var itemType = ResolvedProjectReference.PrimaryDataSourceItemType;
            foreach (string addedSharedImportPath in addedSharedImportPaths)
            {
                var node = RootNode.Children.FindNode(addedSharedImportPath, itemType);
                if (node == null)
                {
                    var sharedFlags = ProjectTreeFlags.Create(ProjectTreeFlags.Common.SharedProjectImportReference);

                    var id = new DependencyNodeId(ProviderType,
                                                  addedSharedImportPath,
                                                  itemType);
                    node = new SharedProjectDependencyNode(id, flags: sharedFlags);
                    dependenciesChange.AddedNodes.Add(node);
                }
            }

            // process removed nodes
            var removedSharedImportPaths = currentSharedImportNodePaths.Except(sharedFolderProjectPaths);
            foreach (string removedSharedImportPath in removedSharedImportPaths)
            {
                var existingImportNode = currentSharedImportNodes
                    .Where(node => PathHelper.IsSamePath(node.Id.ItemSpec, removedSharedImportPath))
                    .FirstOrDefault();

                if (existingImportNode != null)
                {
                    dependenciesChange.RemovedNodes.Add(existingImportNode);
                }
            }
        }
    }
}
