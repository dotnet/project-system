// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides Sdk sub node to global Dependencies project tree node.
    /// </summary>
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class SdkDependenciesSubTreeProvider : DependenciesSubTreeProviderBase
    {
        public const string ProviderTypeString = "SdkDependency";

        public readonly ProjectTreeFlags SdkSubTreeRootNodeFlags
                            = ProjectTreeFlags.Create("SdkSubTreeRootNode");

        public readonly ProjectTreeFlags SdkSubTreeNodeFlags
                            = ProjectTreeFlags.Create("SdkSubTreeNode");

        public SdkDependenciesSubTreeProvider()
        {
            // subscribe to design time build to get corresponding items
            UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(SdkReference.SchemaName);

            ResolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(ResolvedSdkReference.SchemaName);
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
            KnownMonikers.BrowserSDK
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
                                                 Resources.SdkNodeName,
                                                 SdkSubTreeRootNodeFlags,
                                                 KnownMonikers.BrowserSDK);
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
            return new SdkDependencyNode(id,
                                         flags: SdkSubTreeNodeFlags,
                                         priority: priority,
                                         properties: properties,
                                         resolved: resolved);
        }
    }
}
