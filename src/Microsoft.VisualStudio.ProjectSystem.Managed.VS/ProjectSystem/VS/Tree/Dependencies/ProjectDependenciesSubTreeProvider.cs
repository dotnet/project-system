// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
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

        public ProjectDependenciesSubTreeProvider()
        {
            // subscribe to design time build to get corresponding items
            UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(ProjectReference.SchemaName);
            ResolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(ResolvedProjectReference.SchemaName);
        }

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
                                                 Resources.ProjectsNodeName,
                                                 ProjectSubTreeRootNodeFlags,
                                                 KnownMonikers.ApplicationGroup);
        }

        protected override IDependencyNode CreateDependencyNode(string itemSpec,
                                                                string itemType,
                                                                int priority = 0,
                                                                IImmutableDictionary<string, string> properties = null,
                                                                bool resolved = true)
        {
            var id = new DependencyNodeId(ProviderType,
                                          itemSpec,
                                          itemType ?? ResolvedProjectReference.PrimaryDataSourceItemType);
            return new ProjectDependencyNode(id,
                                             flags: ProjectSubTreeNodeFlags,
                                             priority:priority,
                                             properties: properties,
                                             resolved: resolved);
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
