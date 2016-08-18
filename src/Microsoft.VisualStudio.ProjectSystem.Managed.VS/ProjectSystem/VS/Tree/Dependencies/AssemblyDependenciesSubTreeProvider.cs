// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides Assemblies sub node to global Dependencies project tree node.
    /// </summary>
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class AssemblyDependenciesSubTreeProvider : DependenciesSubTreeProviderBase
    {
        public const string ProviderTypeString = "AssemblyDependency";

        public readonly ProjectTreeFlags AssemblySubTreeRootNodeFlags
                            = ProjectTreeFlags.Create("AssemblySubTreeRootNode");

        public readonly ProjectTreeFlags AssemblySubTreeNodeFlags
                            = ProjectTreeFlags.Create("AssemblySubTreeNode");

        public AssemblyDependenciesSubTreeProvider()
        {
            // subscribe to design time build to get corresponding items
            UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(AssemblyReference.SchemaName);
            ResolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(ResolvedAssemblyReference.SchemaName);
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

        private readonly List<ImageMoniker> _nodeIcons = new List<ImageMoniker>
        {
            KnownMonikers.Component
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
                                                 Resources.AssembliesNodeName,
                                                 AssemblySubTreeRootNodeFlags,
                                                 KnownMonikers.Reference);
        }

        protected override IDependencyNode CreateDependencyNode(string itemSpec,
                                                                string itemType,
                                                                int priority = 0,
                                                                IImmutableDictionary<string, string> properties = null,
                                                                bool resolved = true)
        {
            string fusionName = null;
            if (properties != null)
            {
                properties.TryGetValue(ResolvedAssemblyReference.FusionNameProperty, out fusionName);
            }

            var id = new DependencyNodeId(ProviderType,
                                          itemSpec,
                                          itemType ?? ResolvedAssemblyReference.PrimaryDataSourceItemType);

            return new AssemblyDependencyNode(id,
                                              flags: AssemblySubTreeNodeFlags,
                                              priority: priority,
                                              properties: properties,
                                              resolved: resolved,
                                              fusionName: fusionName);
        }
    }
}
