// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides NuGet packages sub node to global Dependencies project tree node.
    /// </summary>
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class NuGetDependenciesSubTreeProvider : DependenciesSubTreeProviderBase
    {
        public const int DiagnosticsNodePriority = 0; // for any custom nodes like errors or warnings
        public const int UnresolvedReferenceNodePriority = 1;
        public const int PackageNodePriority = 2;
        public const int FrameworkAssemblyNodePriority = 3;
        public const int PackageAssemblyNodePriority = 4;

        public const string ProviderTypeString = "NuGetDependency";

        public static readonly ProjectTreeFlags NuGetSubTreeRootNodeFlags
                            = ProjectTreeFlags.Create("NuGetSubTreeRootNode");

        public static readonly ProjectTreeFlags NuGetSubTreeNodeFlags
                            = ProjectTreeFlags.Create("NuGetSubTreeNode")
                                .Union(DependencyNode.CustomItemSpec);

        public NuGetDependenciesSubTreeProvider()
        {
            // subscribe to design time build to get corresponding items

            // For now we don't need unresolved package rules, since we can get all 
            // info from resolved items
            //UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
            //    .Add(PackageReference.SchemaName);
            ResolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(ResolvedPackageReference.SchemaName);
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
            KnownMonikers.PackageReference,
            KnownMonikers.Reference,
            KnownMonikers.ReferenceWarning,
            KnownMonikers.QuestionMark,
            KnownMonikers.Library
        };

        public override IEnumerable<ImageMoniker> Icons
        {
            get
            {
                return _nodeIcons;
            }
        }

        private object _snapshotLock = new object();
        protected DependenciesSnapshot CurrentSnapshot { get; } = new DependenciesSnapshot();

        protected override IDependencyNode CreateRootNode()
        {
            return new SubTreeRootDependencyNode(ProviderType,
                                                 Resources.NuGetPackagesNodeName,
                                                 NuGetSubTreeRootNodeFlags,
                                                 KnownMonikers.PackageReference);
        }

        private IDependencyNode CreateDependencyNode(DependencyMetadata dependencyMetadata, 
                                                     DependencyNodeId id = null,
                                                     bool topLevel = true)
        {
            if (id == null)
            {
                var uniqueToken = topLevel ? null : Guid.NewGuid().ToString();
                id = new DependencyNodeId(ProviderType,
                                          dependencyMetadata.ItemSpec,
                                          ResolvedPackageReference.PrimaryDataSourceItemType,
                                          uniqueToken);
            }

            // here based on DependencyType we create a corresponding node 
            IDependencyNode dependencyNode = null;
            switch(dependencyMetadata.DependencyType)
            {
                case DependencyType.Package:
                    dependencyNode = new PackageDependencyNode(
                                             id,
                                             caption: dependencyMetadata.FriendlyName,
                                             flags: NuGetSubTreeNodeFlags,
                                             properties: dependencyMetadata.Properties,
                                             resolved: dependencyMetadata.Resolved);
                    break;
                case DependencyType.Assembly:
                case DependencyType.FrameworkAssembly:
                    dependencyNode = new PackageAssemblyDependencyNode(
                                             id,
                                             caption: dependencyMetadata.Name,
                                             flags: NuGetSubTreeNodeFlags,
                                             properties: dependencyMetadata.Properties,
                                             resolved: dependencyMetadata.Resolved);
                    break;
                case DependencyType.AnalyzerAssembly:
                    dependencyNode = new PackageAnalyzerAssemblyDependencyNode(
                                             id,
                                             caption: dependencyMetadata.Name,
                                             flags: NuGetSubTreeNodeFlags,
                                             properties: dependencyMetadata.Properties,
                                             resolved: dependencyMetadata.Resolved);
                    break;
                default:
                    dependencyNode = new PackageUnknownDependencyNode(
                                             id,
                                             caption: dependencyMetadata.Name,
                                             flags: NuGetSubTreeNodeFlags,
                                             properties: dependencyMetadata.Properties,
                                             resolved: dependencyMetadata.Resolved);
                    break;
            }

            return dependencyNode;
        }

        private IDependencyNode CreateFrameworkAssembliesFolder()
        {
            var id = new DependencyNodeId(ProviderType,
                                          "FrameworkAssemblies",
                                          ResolvedPackageReference.PrimaryDataSourceItemType,
                                          Guid.NewGuid().ToString());

            return new PackageFrameworkAssembliesDependencyNode(
                                             id,
                                             flags: NuGetSubTreeNodeFlags);
        }
       
        public override IDependencyNode GetDependencyNode(DependencyNodeId id)
        {
            lock (_snapshotLock)
            {
                IDependencyNode node = null;
                if (CurrentSnapshot.NodesCache.TryGetValue(id, out node))
                {
                    return node;
                }

                DependencyMetadata dependencyMetadata = null;
                if (!CurrentSnapshot.DependenciesWorld.TryGetValue(id.ItemSpec, out dependencyMetadata))
                {
                    return null;
                }

                // create node and it's direct children
                node = CreateDependencyNode(dependencyMetadata, id);
                CurrentSnapshot.NodesCache.Add(id, node);

                var frameworkAssemblies = new List<DependencyMetadata>();
                foreach (var childItemSpec in dependencyMetadata.DependenciesItemSpecs)
                {
                    DependencyMetadata childDependencyMetadata = null;
                    if (!CurrentSnapshot.DependenciesWorld.TryGetValue(childItemSpec, out childDependencyMetadata))
                    {
                        continue;
                    }

                    if (childDependencyMetadata.DependencyType == DependencyType.FrameworkAssembly)
                    {
                        frameworkAssemblies.Add(childDependencyMetadata);
                        continue;
                    }

                    node.AddChild(CreateDependencyNode(childDependencyMetadata, topLevel: false));
                }

                if (frameworkAssemblies.Count > 0)
                {
                    var frameworkAssembliesNode = CreateFrameworkAssembliesFolder();
                    node.AddChild(frameworkAssembliesNode);

                    foreach(var fxAssemblyMetadata in frameworkAssemblies)
                    {
                        frameworkAssembliesNode.AddChild(CreateDependencyNode(fxAssemblyMetadata, topLevel: false));
                    }
                }

                return node;
            }
        }

        protected override DependenciesChange ProcessDependenciesChanges(
                                                    IProjectSubscriptionUpdate projectSubscriptionUpdate,
                                                    IProjectCatalogSnapshot catalogs)
        {
            var changes = projectSubscriptionUpdate.ProjectChanges;
            var dependenciesChange = new DependenciesChange();

            lock (_snapshotLock)
            {
                var newDependencies = new HashSet<DependencyMetadata>();
                foreach (var change in changes.Values)
                {
                    if (!change.Difference.AnyChanges)
                    {
                        continue;
                    }

                    foreach (string removedItemSpec in change.Difference.RemovedItems)
                    {
                        CurrentSnapshot.RemoveDependency(removedItemSpec);

                        var itemNode = RootNode.Children.FirstOrDefault(
                                        x => x.Id.ItemSpec.Equals(removedItemSpec, StringComparison.OrdinalIgnoreCase));
                        if (itemNode != null)
                        {
                            dependenciesChange.RemovedNodes.Add(itemNode);
                        }
                    }
                    
                    foreach (string changedItemSpec in change.Difference.ChangedItems)
                    {
                        var properties = GetProjectItemProperties(change.After, changedItemSpec);
                        if (properties == null)
                        {
                            continue;
                        }

                        CurrentSnapshot.UpdateDependency(changedItemSpec, properties);

                        var itemNode = RootNode.Children.FirstOrDefault(
                                        x => x.Id.ItemSpec.Equals(changedItemSpec, StringComparison.OrdinalIgnoreCase));
                        if (itemNode != null)
                        {
                            dependenciesChange.UpdatedNodes.Add(itemNode);
                        }
                    }

                    foreach (string addedItemSpec in change.Difference.AddedItems)
                    {
                        var properties = GetProjectItemProperties(change.After, addedItemSpec);
                        if (properties == null)
                        {
                            continue;
                        }

                        var newDependency = CurrentSnapshot.AddDependency(addedItemSpec, properties);

                        newDependencies.Add(newDependency);
                    }
                }

                // since we have limited implementation for multi targeted projects,
                // we assume that there is only one target - take first target and add 
                // top level nodes for it

                var currentTarget = CurrentSnapshot.Targets.Keys.FirstOrDefault();
                if (currentTarget == null)
                {
                    return dependenciesChange;
                }

                var currentTargetDependency = CurrentSnapshot.DependenciesWorld[currentTarget];
                var currentTargetTopLevelDependencies = currentTargetDependency.DependenciesItemSpecs;
                var addedTopLevelDependencies = newDependencies.Where(
                                                    x => currentTargetTopLevelDependencies.Contains(x.ItemSpec));
                foreach (var addedDependency in addedTopLevelDependencies)
                {
                    var itemNode = RootNode.Children.FirstOrDefault(
                                    x => x.Id.ItemSpec.Equals(addedDependency.ItemSpec, 
                                                              StringComparison.OrdinalIgnoreCase));
                    if (itemNode == null)
                    {
                        itemNode = CreateDependencyNode(addedDependency, topLevel: true);
                        dependenciesChange.AddedNodes.Add(itemNode);
                    }
                }
            }

            return dependenciesChange;
        }

        protected override void ProcessDuplicatedNodes(DependenciesChange dependenciesChange)
        {
            // do nothing - we don't need to do anything special for duplicated nodes,
            // since we make sure that there no duplicated packages earlier.
        }

        public override Task<IEnumerable<IDependencyNode>> SearchAsync(IDependencyNode node, string searchTerm)
        {
            lock (_snapshotLock)
            {
                DependencyMetadata rootDependency = null;
                if (!CurrentSnapshot.DependenciesWorld.TryGetValue(node.Id.ItemSpec, out rootDependency))
                {
                    return Task.FromResult<IEnumerable<IDependencyNode>>(null);
                }

                var flatMatchingDependencies = new HashSet<DependencyMetadata>();
                foreach (var kvp in CurrentSnapshot.DependenciesWorld)
                {
                    if (kvp.Value.Name.ToLowerInvariant().Contains(searchTerm)
                        || kvp.Value.Version.ToLowerInvariant().Contains(searchTerm))
                    {
                        flatMatchingDependencies.Add(kvp.Value);
                    }
                }

                if (flatMatchingDependencies.Count <= 0)
                {
                    return Task.FromResult<IEnumerable<IDependencyNode>>(null);
                }

                return Task.FromResult<IEnumerable<IDependencyNode>>(
                            SearchRecursive(rootDependency, flatMatchingDependencies));
            }
        }

        private IList<IDependencyNode> SearchRecursive(DependencyMetadata rootDependency,
                                                       HashSet<DependencyMetadata> flatMatchingDependencies)
        {
            var matchingNodes = new List<IDependencyNode>();
            foreach(var childDependency in rootDependency.DependenciesItemSpecs)
            {
                DependencyMetadata childDependencyMetadata = null;
                if (!CurrentSnapshot.DependenciesWorld.TryGetValue(childDependency, out childDependencyMetadata))
                {
                    continue;
                }

                var children = SearchRecursive(childDependencyMetadata, flatMatchingDependencies);
                if (children.Count > 0 || flatMatchingDependencies.Contains(childDependencyMetadata))
                {
                    var currentDependencyNode = CreateDependencyNode(childDependencyMetadata, topLevel: false);
                    matchingNodes.Add(currentDependencyNode);
                    foreach(var child in children)
                    {
                        currentDependencyNode.AddChild(child);
                    }
                }
            }

            return matchingNodes;
        }

        protected class TargetMetadata
        {
            public TargetMetadata(IImmutableDictionary<string, string> properties)
            {
                SetProperties(properties);
            }

            public string RuntimeIdentifier { get; private set; }
            public string TargetFrameworkMoniker { get; private set; }
            public string FrameworkName { get; private set; }
            public string FrameworkVersion { get; private set; }

            public void SetProperties(IImmutableDictionary<string, string> properties)
            {
                Requires.NotNull(properties, nameof(properties));

                RuntimeIdentifier = properties.ContainsKey(MetadataKeys.RuntimeIdentifier) 
                                        ? properties[MetadataKeys.RuntimeIdentifier] : string.Empty;
                TargetFrameworkMoniker = properties.ContainsKey(MetadataKeys.TargetFrameworkMoniker) 
                                            ? properties[MetadataKeys.TargetFrameworkMoniker] : string.Empty;
                FrameworkName = properties.ContainsKey(MetadataKeys.FrameworkName)
                                    ? properties[MetadataKeys.FrameworkName] : string.Empty;
                FrameworkVersion = properties.ContainsKey(MetadataKeys.FrameworkVersion) 
                                    ? properties[MetadataKeys.FrameworkVersion] : string.Empty;
            }
        }

        protected class DependencyMetadata
        {
            public DependencyMetadata(string itemSpec, IImmutableDictionary<string, string> properties)
            {
                Requires.NotNull(itemSpec, nameof(itemSpec));
                
                ItemSpec = itemSpec;
                Target = GetTargetFromDependencyId(ItemSpec);

                SetProperties(properties);
            }

            public string Name { get; private set; }
            public string Version { get; private set; }
            public DependencyType DependencyType { get; private set; }
            public string Path { get; private set; }
            public bool Resolved { get; private set; }
            public string ItemSpec { get; set; }
            public string Target { get; }

            public string FriendlyName
            {
                get
                {
                    return $"{Name} ({Version})";
                }
            }

            public IImmutableDictionary<string, string> Properties { get; set; }

            public HashSet<string> DependenciesItemSpecs { get; private set; }

            public void SetProperties(IImmutableDictionary<string, string> properties)
            {
                Requires.NotNull(properties, nameof(properties));

                Name = properties.ContainsKey(MetadataKeys.Name) ? properties[MetadataKeys.Name] : string.Empty;
                Version = properties.ContainsKey(MetadataKeys.Version) 
                                    ? properties[MetadataKeys.Version] : string.Empty;
                var dependencyTypeString = properties.ContainsKey(MetadataKeys.Type) 
                                    ? properties[MetadataKeys.Type] : string.Empty;
                DependencyType dependencyType = DependencyType.Unknown;
                if (Enum.TryParse(dependencyTypeString ?? string.Empty, /*ignoreCase */ true, out dependencyType))
                {
                    DependencyType = dependencyType;
                }

                Path = properties.ContainsKey(MetadataKeys.Path) ? properties[MetadataKeys.Path] : string.Empty;

                bool resolved = true;
                bool.TryParse(properties.ContainsKey(MetadataKeys.Resolved) 
                                ? properties[MetadataKeys.Resolved] : "true", out resolved);
                Resolved = resolved;

                var dependenciesHashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (properties.ContainsKey(MetadataKeys.Dependencies) && properties[MetadataKeys.Dependencies] != null)
                {
                    var dependencyIds = properties[MetadataKeys.Dependencies].Split(new[] { ';' },
                                                                         StringSplitOptions.RemoveEmptyEntries);

                    // store only unique dependency IDs
                    foreach (var dependencyId in dependencyIds)
                    {
                        // store full ids for dependencies to simplify search in DependenciesWorld later
                        dependenciesHashSet.Add($"{Target}/{dependencyId}");
                    }
                }

                DependenciesItemSpecs = dependenciesHashSet;
                Properties = properties;
            }

            public static string GetTargetFromDependencyId(string dependencyId)
            {
                var idParts = dependencyId.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                Requires.NotNull(idParts, nameof(idParts));
                if (idParts.Count() <= 0)
                {
                    // should never happen
                    throw new ArgumentException(nameof(idParts));
                }

                return idParts[0];
            }

            public override int GetHashCode()
            {
                return unchecked(ItemSpec.ToLowerInvariant().GetHashCode());
            }

            public override bool Equals(object obj)
            {
                DependencyMetadata other = obj as DependencyMetadata;
                if (other != null)
                {
                    return Equals(other);
                }
                return false;
            }

            public bool Equals(DependencyMetadata other)
            {
                if (other != null &&
                    other.ItemSpec.Equals(ItemSpec, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }
        }

        protected class DependenciesSnapshot
        {
            public DependenciesSnapshot()
            {
                DependenciesWorld = new Dictionary<string, DependencyMetadata>(StringComparer.OrdinalIgnoreCase);
                Targets = new Dictionary<string, TargetMetadata>(StringComparer.OrdinalIgnoreCase);
                NodesCache = new Dictionary<DependencyNodeId, IDependencyNode>();
            }

            public Dictionary<string, DependencyMetadata> DependenciesWorld { get; }

            public Dictionary<string, TargetMetadata> Targets { get; }

            public Dictionary<DependencyNodeId, IDependencyNode> NodesCache { get; }

            public DependencyMetadata AddDependency(string itemSpec, IImmutableDictionary<string, string> properties)
            {
                if (!itemSpec.Contains("/") && !Targets.ContainsKey(itemSpec))
                {
                    var newTarget = new TargetMetadata(properties);
                    Targets.Add(itemSpec, newTarget);
                }

                var newDependency = new DependencyMetadata(itemSpec, properties);
                DependenciesWorld[itemSpec] = newDependency;

                return newDependency;
            }

            public DependencyMetadata RemoveDependency(string itemSpec)
            {                
                DependencyMetadata removedMetadata = null;
                if (!DependenciesWorld.TryGetValue(itemSpec, out removedMetadata))
                {
                    return null;
                }

                DependenciesWorld.Remove(itemSpec);

                RemoveNodeFromCache(itemSpec);

                return removedMetadata;
            }

            public DependencyMetadata UpdateDependency(string itemSpec, IImmutableDictionary<string,string> properties)
            {
                DependencyMetadata dependencyMetadata = null;
                if (!DependenciesWorld.TryGetValue(itemSpec, out dependencyMetadata))
                {
                    return null;
                }

                if (!itemSpec.Contains("/"))
                {
                    TargetMetadata targetMetadata = null;
                    if (Targets.TryGetValue(itemSpec, out targetMetadata))
                    {
                        targetMetadata.SetProperties(properties);
                    }
                }

                dependencyMetadata.SetProperties(properties);

                RemoveNodeFromCache(itemSpec);

                return dependencyMetadata;
            }

            private void RemoveNodeFromCache(string itemSpec)
            {
                // need to remove dependency from nodes cache
                // need to remove dependency parents from nodes cache
                // need to remove all recursive dependency children from nodes cache

                var nodesToClean = new List<IDependencyNode>();
                foreach (var nodeInCache in NodesCache)
                {
                    if (nodeInCache.Value.Children.Any(x => x.Id.ItemSpec.Equals(itemSpec, StringComparison.OrdinalIgnoreCase))
                        || nodeInCache.Key.ItemSpec.Equals(itemSpec, StringComparison.OrdinalIgnoreCase))
                    {
                        nodesToClean.Add(nodeInCache.Value);
                    }
                }

                foreach (var nodeToClean in nodesToClean)
                {
                    RemoveNodeFromCacheRecursive(nodeToClean);
                }
            }

            private void RemoveNodeFromCacheRecursive(IDependencyNode node)
            {
                NodesCache.Remove(node.Id);

                foreach (var childNode in node.Children)
                {
                    RemoveNodeFromCacheRecursive(childNode);
                }
            }
        }

        protected enum DependencyType
        {
            Unknown,
            Target,
            Diagnostic,
            Package,
            Assembly,
            FrameworkAssembly,
            AnalyzerAssembly
        }

        protected static class MetadataKeys
        {
            // General Metadata
            public const string Name = "Name";
            public const string Type = "Type";
            public const string Version = "Version";
            public const string FileGroup = "FileGroup";
            public const string Path = "Path";
            public const string Resolved = "Resolved";
            public const string Dependencies = "Dependencies";

            // Target Metadata
            public const string RuntimeIdentifier = "RuntimeIdentifier";
            public const string TargetFrameworkMoniker = "TargetFrameworkMoniker";
            public const string FrameworkName = "FrameworkName";
            public const string FrameworkVersion = "FrameworkVersion";
        }
    }
}
