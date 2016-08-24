// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides Analyzers sub node to global Dependencies project tree node.
    /// </summary>
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class AnalyzerDependenciesSubTreeProvider : DependenciesSubTreeProviderBase
    {
        public const string ProviderTypeString = "AnalyzerDependency";

        public readonly ProjectTreeFlags AnalyzerSubTreeRootNodeFlags
                            = ProjectTreeFlags.Create("AnalyzerSubTreeRootNode");

        public readonly ProjectTreeFlags AnalyzerSubTreeNodeFlags
                            = ProjectTreeFlags.Create("AnalyzerSubTreeNode");

        public AnalyzerDependenciesSubTreeProvider()
        {
            // subscribe to design time build to get corresponding items
            UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(AnalyzerReference.SchemaName);
            ResolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(ResolvedAnalyzerReference.SchemaName);
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
            KnownMonikers.CodeInformation
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
                                                 Resources.AnalyzersNodeName,
                                                 AnalyzerSubTreeRootNodeFlags,
                                                 KnownMonikers.CodeInformation);
        }

        protected override IDependencyNode CreateDependencyNode(string itemSpec,
                                                        string itemType,
                                                        int priority = 0,
                                                        IImmutableDictionary<string, string> properties = null,
                                                        bool resolved = true)
        {
            var id = new DependencyNodeId(ProviderType,
                                          itemSpec,
                                          itemType ?? AnalyzerReference.PrimaryDataSourceItemType);

            return new AnalyzerDependencyNode(id,
                                              flags: AnalyzerSubTreeNodeFlags,
                                              priority: priority,
                                              properties: properties,
                                              resolved: resolved);
        }
    }
}
