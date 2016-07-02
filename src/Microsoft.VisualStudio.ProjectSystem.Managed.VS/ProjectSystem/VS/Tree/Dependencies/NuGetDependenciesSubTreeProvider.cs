// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides NuGet packages sub node to global Dependencies project tree node.
    /// </summary>
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class NuGetDependenciesSubTreeProvider : DependenciesSubTreeProviderBase
    {
        public const string NuGetSubTreeRootNodeFlag = "NuGetSubTreeRootNode";
        public const string ProviderTypeString = "NuGetDependency";

        public NuGetDependenciesSubTreeProvider()
        {
            // sucscribe to design time build to get corresponding items
            UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(NuGetPackageReference.SchemaName);
            ResolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(ResolvedNuGetPackageReference.SchemaName);
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
            KnownMonikers.PackageReference
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
            return new DependencyNode(this, 
                                      null /*id*/, 
                                      Resources.NuGetPackagesNodeName, 
                                      KnownMonikers.PackageReference,
                                      itemType: null, 
                                      priority: 0, 
                                      flags: ProjectTreeFlags.Create(NuGetSubTreeRootNodeFlag));
        }

        protected override IDependenciesChangeDiff ProcessDependenciesChanges(
                    IProjectVersionedValue<
                        Tuple<IProjectSubscriptionUpdate,
                              IProjectCatalogSnapshot,
                              IProjectSharedFoldersSnapshot>> e)
        {
            var changes = e.Value.Item1.ProjectChanges;
            var catalogs = e.Value.Item2;
            
            var removedNodes = new List<IDependencyNode>();
            var addedNodes = new List<IDependencyNode>();
            foreach (var change in changes.Values)
            {
                foreach (string removedItem in change.Difference.RemovedItems)
                {
                    // For now differentiate NuGet dependencies by name only in the tree (on the same level)
                    var itemNode = RootNode.Children.FirstOrDefault(
                                    x => x.Caption.Equals(removedItem, StringComparison.OrdinalIgnoreCase));
                    if (itemNode != null)
                    {
                        removedNodes.Add(itemNode);
                        RootNode.RemoveChild(itemNode);
                    }
                }

                foreach (string addedItem in change.Difference.AddedItems)
                {
                    var itemNode = RootNode.Children.FirstOrDefault(
                                    x => x.Caption.Equals(addedItem, StringComparison.OrdinalIgnoreCase));
                    if (itemNode == null)
                    {
                        var properties = GetProjectItemProperties(change.After, addedItem);
                        if (properties != null)
                        {
                            // TODO This is temporary until we determine all data for nuget package dependency
                            // coming from design time build target.
                            properties = properties.Remove(ResolvedNuGetPackageReference.NameProperty)
                                                   .Add(ResolvedNuGetPackageReference.NameProperty, addedItem);
                        }

                        itemNode = new DependencyNode(this, 
                                                      addedItem, 
                                                      addedItem,
                                                      KnownMonikers.PackageReference,
                                                      itemType: ResolvedNuGetPackageReference.PrimaryDataSourceItemType,
                                                      properties: properties,
                                                      priority: 0, 
                                                      flags: ProjectTreeFlags.Empty);
                        RootNode.AddChild(itemNode);
                        addedNodes.Add(itemNode);
                    }
                }
            }

            return new DependenciesChangeDiff(addedNodes, removedNodes);
        }
    }
}
