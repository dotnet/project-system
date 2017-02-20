// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
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

        public static readonly ProjectTreeFlags SdkSubTreeRootNodeFlags
                            = ProjectTreeFlags.Create("SdkSubTreeRootNode");

        public static readonly ProjectTreeFlags SdkSubTreeNodeFlags
                            = ProjectTreeFlags.Create("SdkSubTreeNode");

        [ImportingConstructor]
        public SdkDependenciesSubTreeProvider(INuGetPackagesDataProvider nuGetPackagesSnapshotProvider)
        {
            NuGetPackagesDataProvider = nuGetPackagesSnapshotProvider;

            // subscribe to design time build to get corresponding items
            UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(SdkReference.SchemaName);

            ResolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(ResolvedSdkReference.SchemaName);
        }

        private INuGetPackagesDataProvider NuGetPackagesDataProvider { get; }

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

        public override bool CanDependOnProvider(IProjectDependenciesSubTreeProvider otherProvider)
        {
            return NuGetDependenciesSubTreeProvider.ProviderTypeString
                    .Equals(otherProvider?.ProviderType, StringComparison.OrdinalIgnoreCase);
        }

        protected override IDependencyNode CreateRootNode()
        {
            return new SubTreeRootDependencyNode(ProviderType, 
                                                 VSResources.SdkNodeName,
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
            var flags = SdkSubTreeNodeFlags;
            if (IsImplicit(properties, out string packageItemSpec))
            {
                flags = flags.Union(DependencyNode.DoesNotSupportRemove);
            }

            return new SdkDependencyNode(id,
                                         flags: flags,
                                         priority: priority,
                                         properties: properties,
                                         resolved: resolved);
        }

        public override IDependencyNode GetDependencyNode(DependencyNodeId id)
        {
            var node = base.GetDependencyNode(id);
            if (node == null)
            {
                return null;
            }

            if (!IsImplicit(node, out string packageItemSpec))
            {
                return node;
            }

            NuGetPackagesDataProvider.UpdateNodeChildren(packageItemSpec, node);

            return node;
        }

        public override Task<IEnumerable<IDependencyNode>> SearchAsync(IDependencyNode node, string searchTerm)
        {
            if (!IsImplicit(node, out string packageItemSpec))
            {
                return base.SearchAsync(node, searchTerm);
            }

            return NuGetPackagesDataProvider.SearchAsync(packageItemSpec, searchTerm);
        }

        private bool IsImplicit(IDependencyNode node, out string packageItemSpec)
        {
            return IsImplicit(node.Properties, out packageItemSpec);
        }

        private bool IsImplicit(IImmutableDictionary<string, string> properties, out string packageItemSpec)
        {
            return properties.TryGetValue(SdkReference.SDKPackageItemSpecProperty, out packageItemSpec)
               && !string.IsNullOrEmpty(packageItemSpec);
        }
    }
}
