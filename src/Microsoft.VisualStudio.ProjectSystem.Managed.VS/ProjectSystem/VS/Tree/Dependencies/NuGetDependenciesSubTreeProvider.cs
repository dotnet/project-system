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
    [Export(typeof(INuGetPackagesDataProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class NuGetDependenciesSubTreeProvider : DependenciesSubTreeProviderBase, INuGetPackagesDataProvider
    {
        public const string ProviderTypeString = "NuGetDependency";
        public static readonly ProjectTreeFlags NuGetSubTreeRootNodeFlags
                            = ProjectTreeFlags.Create("NuGetSubTreeRootNode");

        public static readonly ProjectTreeFlags NuGetSubTreeNodeFlags
                            = ProjectTreeFlags.Create("NuGetSubTreeNode")
                                .Union(DependencyNode.CustomItemSpec);

        public NuGetDependenciesSubTreeProvider()
        {
            // subscribe to design time build to get corresponding items
            UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet
                .Add(PackageReference.SchemaName);
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
            KnownMonikers.Library,
            KnownMonikers.CodeInformation
        };

        public override IEnumerable<ImageMoniker> Icons
        {
            get
            {
                return _nodeIcons;
            }
        }

        private object _snapshotLock = new object();

        /// <summary>
        /// Contains a snapshot of package dependencies obtained from nuget assets file. Data here is the
        /// actual resolved packages that should replace package nodes from unresolved TopLevelDependencies.
        /// </summary>
        protected DependenciesSnapshot CurrentSnapshot { get; } = new DependenciesSnapshot();

        /// <summary>
        /// Contains direct top level package dependencies obtained from project evaluation.
        /// </summary>
        protected Dictionary<string, DependencyMetadata> TopLevelDependencies { get; }
            = new Dictionary<string, DependencyMetadata>(StringComparer.OrdinalIgnoreCase);

        protected override IDependencyNode CreateRootNode()
        {
            return new SubTreeRootDependencyNode(ProviderType,
                                                 VSResources.NuGetPackagesNodeName,
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
                                             name: dependencyMetadata.Name,
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
                if (CurrentSnapshot.NodesCache.TryGetValue(id, out IDependencyNode node))
                {
                    return node;
                }

                if (!CurrentSnapshot.DependenciesWorld.TryGetValue(id.ItemSpec, out DependencyMetadata dependencyMetadata))
                {
                    return null;
                }

                return GetDependencyNode(id, dependencyMetadata);
            }
        }

        private IDependencyNode GetDependencyNode(DependencyNodeId id, DependencyMetadata dependencyMetadata)
        {
            // create node and it's direct children
            var node = CreateDependencyNode(dependencyMetadata, id);
            CurrentSnapshot.NodesCache.Add(id, node);

            var frameworkAssemblies = new List<DependencyMetadata>();
            foreach (var childItemSpec in dependencyMetadata.DependenciesItemSpecs)
            {
                if (!CurrentSnapshot.DependenciesWorld.TryGetValue(childItemSpec, out DependencyMetadata childDependencyMetadata))
                {
                    continue;
                }

                if (childDependencyMetadata.DependencyType == DependencyType.FrameworkAssembly)
                {
                    frameworkAssemblies.Add(childDependencyMetadata);
                    continue;
                }
                else if (childDependencyMetadata.Properties.TryGetValue(MetadataKeys.IsImplicitlyDefined, out string isImplicitPackageString)
                    && bool.TryParse(isImplicitPackageString, out bool isImplicitPackage)
                    && isImplicitPackage)
                {
                    // we don't want to show implicit packages at all, since they are SDK's
                    continue;
                }

                node.AddChild(CreateDependencyNode(childDependencyMetadata, topLevel: false));
            }

            if (frameworkAssemblies.Count > 0)
            {
                var frameworkAssembliesNode = CreateFrameworkAssembliesFolder();
                node.AddChild(frameworkAssembliesNode);

                foreach (var fxAssemblyMetadata in frameworkAssemblies)
                {
                    frameworkAssembliesNode.AddChild(CreateDependencyNode(fxAssemblyMetadata, topLevel: false));
                }
            }

            return node;
        }

        protected override DependenciesChange ProcessDependenciesChanges(
                                                     IProjectSubscriptionUpdate projectSubscriptionUpdate,
                                                     IProjectCatalogSnapshot catalogs)
        {
            var dependenciesChange = new DependenciesChange();

            lock (_snapshotLock)
            {
                var rootNodes = RootNode.Children;
                var unresolvedChanges = ProcessUnresolvedChanges(projectSubscriptionUpdate);
                var resolvedChanges = ProcessResolvedChanges(projectSubscriptionUpdate, rootNodes);

                // Logic below merges unresolved and resolved pending changes. Resolved should win if there
                // is similar unresolved dependency. Thus 
                //  - for pending removals, we don't care about the order and just remove whatever valid 
                //    pending changes are there (valid=existing in the RootNode top level children)
                //  - for pending updates, since ItemSpecs must exist (otherwise it would not be an Update pending 
                //    change, but an Add or Remove), we first try to merge resolved pending changes, where for 
                //    each change we try to remove similar unresolved pending Update request.
                //  - for pending additions we need to match itemSpecs of the resolved dependencies to names of the 
                //    unresolved, since resolved ItemSpec is "tfm/packagename/version", but unresolved is just 
                //    "packagename". Thus to avoid same tree node name collision we need to be smart when detecting
                //    similar resolved vs unresolved pending changes.
                //    The algorithm is, first merge resolved changes and if there is a 
                //          - matching unresolved item in the RootNode already, submit a removal 
                //          - matching unresolved item in pending unresolved additions - remove it form there too
                //    Then, process remaining unresolved changes and before mergin each of them, check if matching
                //    name already exists in RootNode or already merged additions.
                //  Note: if it would became too complicated with time, create a separate PackageDependencyChangesResolver
                //  class that would hide it and make logic here simpler.

                // remove
                foreach (var metadata in unresolvedChanges.RemovedNodes)
                {
                    var itemNode = rootNodes.FirstOrDefault(
                                    x => x.Id.ItemSpec.Equals(metadata.ItemSpec, StringComparison.OrdinalIgnoreCase));
                    if (itemNode != null)
                    {
                        dependenciesChange.RemovedNodes.Add(itemNode);
                    }
                }

                foreach (var metadata in resolvedChanges.RemovedNodes)
                {
                    var itemNode = rootNodes.FirstOrDefault(
                                    x => x.Id.ItemSpec.Equals(metadata.ItemSpec, StringComparison.OrdinalIgnoreCase));
                    if (itemNode != null)
                    {
                        dependenciesChange.RemovedNodes.Add(itemNode);
                    }
                }

                // update
                foreach (var resolvedMetadata in resolvedChanges.UpdatedNodes)
                {
                    // since it is an update root node must have those item specs, so we can check them
                    var itemNode = rootNodes.FirstOrDefault(
                                    x => x.Id.ItemSpec.Equals(resolvedMetadata.ItemSpec, StringComparison.OrdinalIgnoreCase));
                    if (itemNode != null)
                    {
                        itemNode = CreateDependencyNode(resolvedMetadata, topLevel: true);
                        dependenciesChange.UpdatedNodes.Add(itemNode);
                    }

                    var unresolvedMatch = unresolvedChanges.UpdatedNodes.FirstOrDefault(x => x.ItemSpec.Equals(resolvedMetadata.Name));
                    if (unresolvedMatch != null)
                    {
                        unresolvedChanges.UpdatedNodes.Remove(unresolvedMatch);
                    }
                }

                foreach (var unresolvedMetadata in unresolvedChanges.UpdatedNodes)
                {
                    // since it is an update root node must have those item specs, so we can check them
                    var itemNode = rootNodes.FirstOrDefault(
                                    x => x.Id.ItemSpec.Equals(unresolvedMetadata.ItemSpec, StringComparison.OrdinalIgnoreCase));
                    if (itemNode != null)
                    {
                        dependenciesChange.UpdatedNodes.Add(itemNode);
                    }
                }

                // add
                foreach (var resolvedMetadata in resolvedChanges.AddedNodes)
                {
                    // see if there is already node created for unresolved package - if yes, delete it
                    // Note: unresolved packages ItemSpec contain only package name, when resolved package ItemSpec
                    // contains TFM/PackageName/version, so we need to check for name if we want to find unresolved
                    // packages.
                    var itemNode = rootNodes.FirstOrDefault(
                                    x => x.Id.ItemSpec.Equals(resolvedMetadata.Name, StringComparison.OrdinalIgnoreCase));
                    if (itemNode != null)
                    {
                        dependenciesChange.RemovedNodes.Add(itemNode);
                    }

                    // see if there no node with the same resolved metadata - if no, create it
                    itemNode = rootNodes.FirstOrDefault(
                                    x => x.Id.ItemSpec.Equals(resolvedMetadata.ItemSpec, StringComparison.OrdinalIgnoreCase));
                    if (itemNode == null)
                    {
                        itemNode = CreateDependencyNode(resolvedMetadata, topLevel: true);
                        dependenciesChange.AddedNodes.Add(itemNode);
                    }

                    // avoid adding matching unresolved packages
                    var unresolvedMatch = unresolvedChanges.AddedNodes.FirstOrDefault(x => x.ItemSpec.Equals(resolvedMetadata.Name));
                    if (unresolvedMatch != null)
                    {
                        unresolvedChanges.AddedNodes.Remove(unresolvedMatch);
                    }
                }

                foreach (var unresolvedMetadata in unresolvedChanges.AddedNodes)
                {
                    var itemNode = rootNodes.FirstOrDefault(x => DoesNodeMatchByNameOrItemSpec(x, unresolvedMetadata.ItemSpec));
                    if (itemNode == null)
                    {
                        // in case when unresolved come together with resolved data, root nodes might not yet have 
                        // an unresolved node and we need to check if we did add resolved one above to avoid collision.
                        itemNode = dependenciesChange.AddedNodes.FirstOrDefault(
                                        x => DoesNodeMatchByNameOrItemSpec(x, unresolvedMetadata.ItemSpec));
                    }

                    if (itemNode == null)
                    {
                        itemNode = CreateDependencyNode(unresolvedMetadata, topLevel: true);
                        dependenciesChange.AddedNodes.Add(itemNode);
                    }
                }                
            }

            return dependenciesChange;
        }

        private bool DoesNodeMatchByNameOrItemSpec(IDependencyNode node, string itemSpecToCheck)
        {
            bool? result = false;
            if (CurrentSnapshot.DependenciesWorld.TryGetValue(node.Id.ItemSpec, out DependencyMetadata metadata))
            {
                result = metadata?.Name?.Equals(itemSpecToCheck, StringComparison.OrdinalIgnoreCase);
            }

            if (!result.HasValue || !result.Value)
            {
                result = node.Id.ItemSpec.Equals(itemSpecToCheck, StringComparison.OrdinalIgnoreCase);
            }

            return result.Value;
        }

        private NugetDependenciesChange ProcessUnresolvedChanges(IProjectSubscriptionUpdate projectSubscriptionUpdate)
        {
            var unresolvedChanges = projectSubscriptionUpdate.ProjectChanges.Values
                .Where(cd => !ResolvedReferenceRuleNames.Any(ruleName =>
                                string.Equals(ruleName,
                                              cd.After.RuleName,
                                              StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(d => d.After.RuleName, d => d, StringComparer.OrdinalIgnoreCase);

            var dependenciesChange = new NugetDependenciesChange();
            foreach (var change in unresolvedChanges.Values)
            {
                if (!change.Difference.AnyChanges)
                {
                    continue;
                }

                foreach (string removedItemSpec in change.Difference.RemovedItems)
                {
                    if (TopLevelDependencies.TryGetValue(removedItemSpec, out DependencyMetadata metadata))
                    {
                        TopLevelDependencies.Remove(removedItemSpec);
                        dependenciesChange.RemovedNodes.Add(metadata);
                    }
                }

                foreach (string changedItemSpec in change.Difference.ChangedItems)
                {
                    var properties = GetProjectItemProperties(change.After, changedItemSpec);
                    if (properties == null)
                    {
                        continue;
                    }

                    if (TopLevelDependencies.TryGetValue(changedItemSpec, out DependencyMetadata metadata))
                    {
                        metadata = CreateUnresolvedMetadata(changedItemSpec, properties);
                        TopLevelDependencies[changedItemSpec] = metadata;

                        dependenciesChange.UpdatedNodes.Add(metadata);
                    }
                }

                foreach (string addedItemSpec in change.Difference.AddedItems)
                {
                    var properties = GetProjectItemProperties(change.After, addedItemSpec);
                    if (properties == null)
                    {
                        continue;
                    }

                    if (!TopLevelDependencies.TryGetValue(addedItemSpec, out DependencyMetadata metadata))
                    {
                        metadata = CreateUnresolvedMetadata(addedItemSpec, properties);
                        if (metadata.IsImplicitlyDefined)
                        {
                            continue;
                        }

                        TopLevelDependencies.Add(addedItemSpec, metadata);

                        dependenciesChange.AddedNodes.Add(metadata);
                    }
                }
            }

            return dependenciesChange;
        }

        private NugetDependenciesChange ProcessResolvedChanges(IProjectSubscriptionUpdate projectSubscriptionUpdate,
                                                               ImmutableHashSet<IDependencyNode> rootTreeNodes)
        {
            var changes = projectSubscriptionUpdate.ProjectChanges;
            var resolvedChanges = ResolvedReferenceRuleNames.Where(x => changes.Keys.Contains(x))
                                                            .Select(ruleName => changes[ruleName])
                                                            .ToImmutableHashSet();
            var dependenciesChange = new NugetDependenciesChange();
            var newDependencies = new HashSet<DependencyMetadata>();
            foreach (var change in resolvedChanges)
            {
                if (!change.Difference.AnyChanges)
                {
                    continue;
                }

                foreach (string removedItemSpec in change.Difference.RemovedItems)
                {
                    var metadata = CurrentSnapshot.RemoveDependency(removedItemSpec);
                    var itemNode = rootTreeNodes.FirstOrDefault(
                                    x => x.Id.ItemSpec.Equals(removedItemSpec, StringComparison.OrdinalIgnoreCase));
                    if (itemNode != null)
                    {
                        dependenciesChange.RemovedNodes.Add(metadata);
                    }
                }

                foreach (string changedItemSpec in change.Difference.ChangedItems)
                {
                    var properties = GetProjectItemProperties(change.After, changedItemSpec);
                    if (properties == null)
                    {
                        continue;
                    }

                    var metadata = CurrentSnapshot.UpdateDependency(changedItemSpec, properties);
                    var itemNode = rootTreeNodes.FirstOrDefault(
                                    x => x.Id.ItemSpec.Equals(changedItemSpec, StringComparison.OrdinalIgnoreCase));
                    if (itemNode != null)
                    {
                        dependenciesChange.UpdatedNodes.Add(metadata);
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

            // Note: currently deisgn time build is limited and is not aware of conditional on TFM 
            // PackageReference items: Unresolved PackageReference items for conditional TFMs are not sent.
            // Thus we will display conditional PackageReferences if they were resolved and are in assets.json.
            // This limitation should go away, when we have final design for cross target dependencies and 
            // DesignTime build.
            var allTargetsDependencies = CurrentSnapshot.GetUniqueTopLevelDependencies();
            if (allTargetsDependencies.Count == 0)
            {
                return dependenciesChange;
            }

            var addedTopLevelDependencies = newDependencies.Where(
                x => allTargetsDependencies.Contains(x.ItemSpec) && !x.IsImplicitlyDefined);
            foreach (var addedDependency in addedTopLevelDependencies)
            {
                dependenciesChange.AddedNodes.Add(addedDependency);
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
            return SearchAsync(node.Id.ItemSpec, searchTerm);
        }

        private IList<IDependencyNode> SearchRecursive(DependencyMetadata rootDependency,
                                                       HashSet<DependencyMetadata> flatMatchingDependencies)
        {
            var matchingNodes = new List<IDependencyNode>();
            foreach(var childDependency in rootDependency.DependenciesItemSpecs)
            {
                if (!CurrentSnapshot.DependenciesWorld.TryGetValue(childDependency, out DependencyMetadata childDependencyMetadata))
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

        // INuGetPackagesDataProvider methods

        /// <summary>
        /// Allows to other providers to use nuget package dependencies search on a given node if it 
        /// turns out to be a nuget package.
        /// </summary>
        /// <param name="packageItemSpec">Package reference items spec for which we need to do a search</param>
        /// <param name="searchTerm">String to be searched</param>
        /// <returns></returns>
        public Task<IEnumerable<IDependencyNode>> SearchAsync(string packageItemSpec, string searchTerm)
        {
            lock (_snapshotLock)
            {
                if (!CurrentSnapshot.DependenciesWorld.TryGetValue(packageItemSpec, out DependencyMetadata rootDependency))
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

        /// <summary>
        /// For given node tries to find matching nuget package and adds direct package dependencies
        /// nodes to given node's children collection. Use case is, when other provider has a node,
        /// that is actually a package, it would call this method when GraphProvider is about to 
        /// check if node has children or not.
        /// </summary>
        /// <param name="packageItemSpec">Package reference items spec that is supposed to be associated 
        /// with the given node</param>
        /// <param name="originalNode">Node to fill children for, if it's is a package</param>
        public void UpdateNodeChildren(string packageItemSpec, IDependencyNode originalNode)
        {
            lock (_snapshotLock)
            {
                originalNode.RemoveAllChildren();

                if (!CurrentSnapshot.NodesCache.TryGetValue(originalNode.Id, out IDependencyNode node))
                {
                    if (!CurrentSnapshot.DependenciesWorld.TryGetValue(packageItemSpec, out DependencyMetadata dependencyMetadata))
                    {
                        return;
                    }

                    node = GetDependencyNode(originalNode.Id, dependencyMetadata);
                }

                if (node == null)
                {
                    return;
                }

                var nodeChildren = node.Children;
                foreach (var child in nodeChildren)
                {
                    originalNode.AddChild(child);
                }
            }
        }

        // End of INuGetPackagesDataProvider methods

        private static DependencyMetadata CreateUnresolvedMetadata(string itemSpec,
                                                            IImmutableDictionary<string, string> properties)
        {
            // add this properties here since unresolved PackageReferences don't have it
            properties = properties.SetItem(MetadataKeys.Resolved, "false");
            properties = properties.SetItem(MetadataKeys.Type, DependencyType.Package.ToString());

            return new DependencyMetadata(itemSpec, properties);
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
            public bool IsImplicitlyDefined { get; private set; }

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

                Name = properties.ContainsKey(MetadataKeys.Name) && !string.IsNullOrEmpty(properties[MetadataKeys.Name]) 
                            ? properties[MetadataKeys.Name] 
                            : ItemSpec;
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

                bool.TryParse(properties.ContainsKey(MetadataKeys.Resolved)
                    ? properties[MetadataKeys.Resolved] : "true", out bool resolved);
                Resolved = resolved;

                bool.TryParse(properties.ContainsKey(MetadataKeys.IsImplicitlyDefined)
                    ? properties[MetadataKeys.IsImplicitlyDefined] : "false", out bool isImplicitlyDefined);
                IsImplicitlyDefined = isImplicitlyDefined;

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
                if (obj is DependencyMetadata other)
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
                if (!DependenciesWorld.TryGetValue(itemSpec, out DependencyMetadata removedMetadata))
                {
                    return null;
                }

                DependenciesWorld.Remove(itemSpec);

                RemoveNodeFromCache(itemSpec);

                if (!itemSpec.Contains("/") && Targets.ContainsKey(itemSpec))
                {
                    Targets.Remove(itemSpec);
                }

                return removedMetadata;
            }

            public DependencyMetadata UpdateDependency(string itemSpec, IImmutableDictionary<string,string> properties)
            {
                if (!DependenciesWorld.TryGetValue(itemSpec, out DependencyMetadata dependencyMetadata))
                {
                    return null;
                }

                if (!itemSpec.Contains("/"))
                {
                    if (Targets.TryGetValue(itemSpec, out TargetMetadata targetMetadata))
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
                    var nodeChildren = nodeInCache.Value.Children;
                    if (nodeChildren.Any(x => x.Id.ItemSpec.Equals(itemSpec, StringComparison.OrdinalIgnoreCase))
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

                var nodeChildren = node.Children;
                foreach (var childNode in nodeChildren)
                {
                    RemoveNodeFromCacheRecursive(childNode);
                }
            }

            /// <summary>
            /// Until we have a proper design for displaying cross-target dependencies, we show all
            /// in one list. 
            /// </summary>
            /// <returns></returns>
            public HashSet<string> GetUniqueTopLevelDependencies()
            {
                HashSet<string> topLevelDependenciesNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> topLevelDependenciesItemSpecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(var target in Targets)
                {
                    if (!DependenciesWorld.TryGetValue(target.Key, out DependencyMetadata targetMetadata))
                    {
                        continue;
                    }

                    foreach (var dependencyItemSpec in targetMetadata.DependenciesItemSpecs)
                    {
                        if (!DependenciesWorld.TryGetValue(dependencyItemSpec, out DependencyMetadata dependencyMetadata))
                        {
                            continue;
                        }

                        if (topLevelDependenciesNames.Contains(dependencyMetadata.Name))
                        {
                            // we already have this dependency form other target
                            continue;
                        }

                        topLevelDependenciesNames.Add(dependencyMetadata.Name);
                        topLevelDependenciesItemSpecs.Add(dependencyItemSpec);
                    }
                }

                return topLevelDependenciesItemSpecs;
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
            public const string IsImplicitlyDefined = "IsImplicitlyDefined";

            // Target Metadata
            public const string RuntimeIdentifier = "RuntimeIdentifier";
            public const string TargetFrameworkMoniker = "TargetFrameworkMoniker";
            public const string FrameworkName = "FrameworkName";
            public const string FrameworkVersion = "FrameworkVersion";
        }

        protected class NugetDependenciesChange
        {
            public NugetDependenciesChange()
            {
                AddedNodes = new List<DependencyMetadata>();
                UpdatedNodes = new List<DependencyMetadata>();
                RemovedNodes = new List<DependencyMetadata>();
            }

            public List<DependencyMetadata> AddedNodes { get; protected set; }
            public List<DependencyMetadata> UpdatedNodes { get; protected set; }
            public List<DependencyMetadata> RemovedNodes { get; protected set; }
        }
    }
}
