// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

/// <summary>
/// Populates and updates an <see cref="IProjectTree"/> from a <see cref="DependenciesSnapshot"/>.
/// </summary>
/// <remarks>
/// This class's primary entry point is <see cref="BuildTreeAsync"/>.
/// </remarks>
[Export]
internal sealed class DependenciesTreeBuilder
{
    private static ImmutableDictionary<ProjectConfigurationSlice, ProjectTreeFlags> s_flagsByConfigurationSlice = ImmutableDictionary<ProjectConfigurationSlice, ProjectTreeFlags>.Empty;

    private readonly UnconfiguredProject _unconfiguredProject;

#if false
    private DependenciesSnapshot? _lastSnapshot;
#endif

    /// <summary>
    /// <see cref="IProjectTreePropertiesProvider"/> imports that apply to the references tree.
    /// </summary>
    [ImportMany(ReferencesProjectTreeCustomizablePropertyValues.ContractName)]
    private readonly OrderPrecedenceImportCollection<IProjectTreePropertiesProvider> _projectTreePropertiesProviders;

    [Import]
    internal IProjectTreeOperations TreeConstruction { get; set; } = null!;

    [ImportingConstructor]
    public DependenciesTreeBuilder(UnconfiguredProject unconfiguredProject)
    {
        _unconfiguredProject = unconfiguredProject;

        _projectTreePropertiesProviders = new OrderPrecedenceImportCollection<IProjectTreePropertiesProvider>(
            ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
            projectCapabilityCheckProvider: unconfiguredProject);
    }

    /// <summary>
    /// Builds the "Dependencies" node <see cref="IProjectTree"/> for the given <paramref name="snapshot"/> based on the previous <paramref name="dependenciesNode"/>.
    /// </summary>
    /// <param name="dependenciesNode">The previous dependencies tree, to which the updated <paramref name="snapshot"/> should be applied.</param>
    /// <param name="snapshot">The current dependencies snapshot to apply to the tree.</param>
    /// <param name="cancellationToken">Supports cancellation of this operation.</param>
    /// <returns>An updated "Dependencies" node.</returns>
    internal async Task<IProjectTree> BuildTreeAsync(
        IProjectTree? dependenciesNode,
        DependenciesSnapshot snapshot,
        CancellationToken cancellationToken)
    {
#if false
        // TODO make this condition pass reliably, so we know we are not performing redundant updates
        System.Diagnostics.Debug.Assert(!Equals(snapshot, _lastSnapshot), "Should not receive the same snapshot object twice!");
        _lastSnapshot = snapshot;
#endif

        // If we don't yet have a tree, create the root node.
        dependenciesNode ??= CreateDependenciesNode();

        // Keep a reference to the original tree to return in case we are cancelled.
        IProjectTree originalNode = dependenciesNode;

        var expectedChildren = new HashSet<IProjectTree>();

        if (snapshot.DependenciesBySlice.Count == 1)
        {
            await BuildSingleSliceTreeAsync();
        }
        else if (snapshot.DependenciesBySlice.Count > 1)
        {
            await BuildMultiSliceTreeAsync();
        }

        await BuildUnconfiguredNodesAsync();

        dependenciesNode = RemoveUnexpectedChildren(dependenciesNode, expectedChildren);

        ProjectImageMoniker rootIcon = snapshot.MaximumDiagnosticLevel switch
        {
            DiagnosticLevel.Error => KnownProjectImageMonikers.ReferenceGroupError,
            DiagnosticLevel.Warning => KnownProjectImageMonikers.ReferenceGroupWarning,
            _ => KnownProjectImageMonikers.ReferenceGroup
        };

        if (cancellationToken.IsCancellationRequested)
        {
            // We return the original tree on cancellation. This is because the cancellation can indicate
            // that a newer update is pending (due to debouncing of updates), rather than unloading of the
            // project. In such cases we don't want to throw. We just want the current update to be treated
            // as a no-op, after which a subsequent update will occur. When we return the same object
            // unmodified, CPS will not perform additional work as part of the update.
            return originalNode;
        }

        if (dependenciesNode.Icon == rootIcon && dependenciesNode.ExpandedIcon == rootIcon)
        {
            // The icon is unchanged, so avoid creating an additional tree item.
            return dependenciesNode;
        }
        else
        {
            // The icon changed. Apply it.
            return dependenciesNode.SetProperties(icon: rootIcon, expandedIcon: rootIcon);
        }

        IProjectTree CreateDependenciesNode()
        {
            var values = new ReferencesProjectTreeCustomizablePropertyValues
            {
                Caption = Resources.DependenciesNodeName,
                Icon = KnownProjectImageMonikers.ReferenceGroup,
                ExpandedIcon = KnownProjectImageMonikers.ReferenceGroup,
                Flags = DependencyTreeFlags.DependenciesRootNodeFlags
            };

            // Allow property providers to perform customization.
            // These are ordered from lowest priority to highest, allowing higher priority
            // providers to override lower priority providers.
            foreach (IProjectTreePropertiesProvider provider in _projectTreePropertiesProviders.ExtensionValues())
            {
                provider.CalculatePropertyValues(ProjectTreeCustomizablePropertyContext.Default, values);
            }

            return TreeConstruction.NewTree(
                caption: values.Caption,
                icon: values.Icon,
                expandedIcon: values.ExpandedIcon,
                flags: values.Flags);
        }

        async Task BuildSingleSliceTreeAsync()
        {
            // NOTE even though we have a single slice, it might not be empty.
            // If a project uses "<TargetFrameworks>net8.0</TargetFrameworks>" (plural) then
            // we will have a single dimension in our single slice.
            foreach ((_, DependenciesSnapshotSlice snapshotSlice) in snapshot.DependenciesBySlice)
            {
                dependenciesNode = await PopulateConfiguredDependencyGroupsAsync(
                    parentNode: dependenciesNode,
                    snapshot.PrimarySlice,
                    snapshotSlice,
                    RememberNewNodes);
            }
        }

        async Task BuildMultiSliceTreeAsync()
        {
            foreach ((ProjectConfigurationSlice slice, DependenciesSnapshotSlice snapshotSlice) in snapshot.DependenciesBySlice)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                string caption = GetSliceCaption(slice);

                // TODO is there a better way to find an existing slice node?
                IProjectTree? sliceNode = dependenciesNode.FindChildWithCaption(caption);

                bool shouldAddSliceNode = sliceNode is null;

                sliceNode = CreateOrUpdateNode(
                    parentNode: dependenciesNode,
                    node: sliceNode,
                    caption: caption,
                    flags: GetSliceFlags(slice),
                    icon: GetSliceIcon(snapshotSlice),
                    isProjectItem: false,
                    browseObjectProperties: null);

                sliceNode = await PopulateConfiguredDependencyGroupsAsync(
                    parentNode: sliceNode,
                    snapshot.PrimarySlice,
                    snapshotSlice,
                    RemoveUnexpectedChildren);

                dependenciesNode = shouldAddSliceNode
                    ? dependenciesNode.Add(sliceNode).Parent!
                    : sliceNode.Parent!;

                Assumes.NotNull(dependenciesNode);

                expectedChildren.Add(sliceNode);
            }

            static ProjectTreeFlags GetSliceFlags(ProjectConfigurationSlice slice)
            {
                return ImmutableInterlocked.GetOrAdd(
                    ref s_flagsByConfigurationSlice,
                    slice,
                    static slice =>
                    {
                        ProjectTreeFlags flags = DependencyTreeFlags.TargetNodeFlags;

                        if (slice.Dimensions.TryGetValue(ConfigurationGeneral.TargetFrameworkProperty, out string? targetFramework))
                        {
                            flags = flags.Add($"$TFM:{targetFramework}");
                        }

                        return flags;
                    });
            }

            static string GetSliceCaption(ProjectConfigurationSlice slice)
            {
                Assumes.False(slice.Dimensions.Count == 0);

                if (slice.Dimensions.Count == 1)
                {
                    return slice.Dimensions.First().Value;
                }

                return string.Join(" | ", slice.Dimensions.Values);
            }

            static ProjectImageMoniker GetSliceIcon(DependenciesSnapshotSlice snapshotSlice)
            {
                return snapshotSlice.MaximumDiagnosticLevel switch
                {
                    DiagnosticLevel.Error => KnownProjectImageMonikers.LibraryError,
                    DiagnosticLevel.Warning => KnownProjectImageMonikers.LibraryWarning,
                    _ => KnownProjectImageMonikers.Library
                };
            }
        }

        async Task BuildUnconfiguredNodesAsync()
        {
            foreach ((DependencyGroupType groupType, ImmutableArray<IDependency> dependencies) in snapshot.UnconfiguredDependenciesByType)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                IProjectTree? groupNode = dependenciesNode.FindChildWithCaption(groupType.Caption);
                bool shouldAddGroupNode = groupNode is null;

                groupNode = CreateOrUpdateNode(
                    parentNode: dependenciesNode,
                    node: groupNode,
                    caption: groupType.Caption,
                    flags: groupType.GroupNodeFlags,
                    icon: GetGroupIcon(dependencies, groupType),
                    isProjectItem: false);

                groupNode = await PopulateGroupNodeAsync(
                    groupNode,
                    dependencies,
                    shouldCleanup: !shouldAddGroupNode);

                expectedChildren.Add(groupNode);

                dependenciesNode = shouldAddGroupNode
                    ? dependenciesNode.Add(groupNode).Parent!
                    : groupNode.Parent!;

                Assumes.NotNull(dependenciesNode);
            }

            static ProjectImageMoniker GetGroupIcon(ImmutableArray<IDependency> dependencies, DependencyGroupType groupType)
            {
                return GetMaximumDiagnosticLevel() switch
                {
                    DiagnosticLevel.Error => groupType.ErrorGroupIcon,
                    DiagnosticLevel.Warning => groupType.WarningGroupIcon,
                    _ => groupType.NormalGroupIcon
                };

                DiagnosticLevel GetMaximumDiagnosticLevel()
                {
                    DiagnosticLevel max = DiagnosticLevel.None;

                    foreach (IDependency dependency in dependencies)
                    {
                        if (dependency.DiagnosticLevel > max)
                        {
                            max = dependency.DiagnosticLevel;
                        }
                    }

                    return max;
                }
            }

            async Task<IProjectTree> PopulateGroupNodeAsync(
                IProjectTree groupNode,
                ImmutableArray<IDependency> dependencies,
                bool shouldCleanup)
            {
                HashSet<IProjectTree>? expectedChildren = shouldCleanup
                    ? new HashSet<IProjectTree>(capacity: dependencies.Length)
                    : null;

                foreach (IDependency dependency in dependencies)
                {
                    IProjectTree? dependencyNode = groupNode.FindChildForDependency(dependency);
                    bool isNewDependencyNode = dependencyNode is null;

                    var dependencyWithBrowseObject = dependency as IDependencyWithBrowseObject;

                    IRule? browseObjectProperties = dependencyWithBrowseObject is not null
                        ? await TreeConstruction.GetDependencyBrowseObjectRuleAsync(dependencyWithBrowseObject, configuredProject: null, catalogs: null)
                        : null;

                    dependencyNode = CreateOrUpdateNode(
                        groupNode,
                        dependencyNode,
                        dependency.Caption,
                        dependency.Flags,
                        dependency.Icon,
                        isProjectItem: false, // Don't expose unconfigured dependencies via DTE
                        browseObjectProperties,
                        dependencyWithBrowseObject?.FilePath,
                        dependencyWithBrowseObject?.SchemaItemType);

                    expectedChildren?.Add(dependencyNode);

                    IProjectTree? parent = isNewDependencyNode
                        ? groupNode.Add(dependencyNode).Parent
                        : dependencyNode.Parent;

                    Assumes.NotNull(parent);

                    groupNode = parent;
                }

                return expectedChildren is not null
                    ? RemoveUnexpectedChildren(groupNode, expectedChildren)
                    : groupNode;
            }
        }

        IProjectTree RememberNewNodes(IProjectTree rootNode, IEnumerable<IProjectTree> newNodes)
        {
            if (newNodes is not null)
            {
                expectedChildren.AddRange(newNodes);
            }

            return rootNode;
        }

        // Populates configured (within a slice) dependency group nodes beneath the specified parent.
        async Task<IProjectTree> PopulateConfiguredDependencyGroupsAsync(
            IProjectTree parentNode,
            ProjectConfigurationSlice primarySlice,
            DependenciesSnapshotSlice snapshotSlice,
            Func<IProjectTree, HashSet<IProjectTree>, IProjectTree> syncFunc)
        {
            var actualChildren = new HashSet<IProjectTree>(capacity: snapshotSlice.DependenciesByType.Count);

            bool isPrimarySlice = snapshotSlice.Slice.Equals(primarySlice);

            foreach ((DependencyGroupType groupType, ImmutableArray<IDependency> dependencies) in snapshotSlice.DependenciesByType)
            {
                IProjectTree? groupNode = parentNode.FindChildWithFlags(groupType.GroupNodeFlags);
                bool shouldAddGroupNode = groupNode is null;

                groupNode = CreateOrUpdateNode(
                    parentNode: parentNode,
                    node: groupNode,
                    caption: groupType.Caption,
                    flags: groupType.GroupNodeFlags,
                    icon: GetGroupIcon(snapshotSlice, groupType),
                    isProjectItem: false);

                groupNode = await PopulateGroupNodeAsync(
                    groupNode,
                    snapshotSlice,
                    dependencies,
                    isPrimarySlice,
                    shouldCleanup: !shouldAddGroupNode);

                actualChildren.Add(groupNode);

                parentNode = shouldAddGroupNode
                    ? parentNode.Add(groupNode).Parent!
                    : groupNode.Parent!;

                Assumes.NotNull(parentNode);
            }

            return syncFunc(parentNode, actualChildren);

            static ProjectImageMoniker GetGroupIcon(DependenciesSnapshotSlice snapshotSlice, DependencyGroupType groupType)
            {
                return snapshotSlice.GetMaximumDiagnosticLevelForDependencyGroupType(groupType) switch
                {
                    DiagnosticLevel.Error => groupType.ErrorGroupIcon,
                    DiagnosticLevel.Warning => groupType.WarningGroupIcon,
                    _ => groupType.NormalGroupIcon
                };
            }

            async Task<IProjectTree> PopulateGroupNodeAsync(
                IProjectTree groupNode,
                DependenciesSnapshotSlice snapshotSlice,
                ImmutableArray<IDependency> dependencies,
                bool isPrimarySlice,
                bool shouldCleanup)
            {
                HashSet<IProjectTree>? expectedChildren = shouldCleanup
                    ? new HashSet<IProjectTree>(capacity: dependencies.Length)
                    : null;

                foreach (IDependency dependency in dependencies)
                {
                    IProjectTree? dependencyNode = groupNode.FindChildForDependency(dependency);
                    bool isNewDependencyNode = dependencyNode is null;

                    // NOTE this project system supports multiple implicit configuration dimensions (such as target framework)
                    // which is a concept not modelled by DTE/VSLangProj. In order to produce a sensible view of the project
                    // via automation, we expose only the primary slice's dependencies at any given time.
                    //
                    // This is achieved by using IProjectItemTree for primary slice items, and IProjectTree for items in
                    // other slices. CPS only creates automation objects for items with "Reference" flag if they implement
                    // IProjectItemTree. See SimpleItemNode.Initialize (in CPS) for details.

                    dependencyNode = await CreateOrUpdateDependencyNodeAsync(
                        parentNode: groupNode,
                        dependencyNode,
                        dependency,
                        snapshotSlice,
                        isProjectItem: isPrimarySlice);

                    expectedChildren?.Add(dependencyNode);

                    IProjectTree? parent = isNewDependencyNode
                        ? groupNode.Add(dependencyNode).Parent
                        : dependencyNode.Parent;

                    Assumes.NotNull(parent);

                    groupNode = parent;
                }

                return expectedChildren is not null
                    ? RemoveUnexpectedChildren(groupNode, expectedChildren)
                    : groupNode;
            }
        }

        static IProjectTree RemoveUnexpectedChildren(IProjectTree parent, HashSet<IProjectTree> expectedChildren)
        {
            foreach (IProjectTree child in parent.Children)
            {
                if (!expectedChildren.Contains(child))
                {
                    parent = parent.Remove(child);
                }
            }

            return parent;
        }

        async ValueTask<IProjectTree> CreateOrUpdateDependencyNodeAsync(
            IProjectTree parentNode,
            IProjectTree? node,
            IDependency dependency,
            DependenciesSnapshotSlice snapshotSlice,
            bool isProjectItem)
        {
            var dependencyWithBrowseObject = dependency as IDependencyWithBrowseObject;

            IRule? browseObjectProperties = dependencyWithBrowseObject is not null
                ? await TreeConstruction.GetDependencyBrowseObjectRuleAsync(dependencyWithBrowseObject, snapshotSlice.ConfiguredProject, snapshotSlice.Catalogs)
                : null;

            return CreateOrUpdateNode(
                parentNode,
                node,
                dependency.Caption,
                dependency.Flags,
                dependency.Icon,
                isProjectItem,
                browseObjectProperties,
                dependencyWithBrowseObject?.FilePath,
                dependencyWithBrowseObject?.SchemaItemType);
        }

        IProjectTree CreateOrUpdateNode(
            IProjectTree parentNode,
            IProjectTree? node,
            string caption,
            ProjectTreeFlags flags,
            ProjectImageMoniker icon,
            bool isProjectItem,
            IRule? browseObjectProperties = null,
            string? filePath = null,
            string? schemaItemType = null)
        {
            FilterProperties();

            if (node is not null)
            {
                return UpdateTreeNode();
            }
            else if (isProjectItem)
            {
                // This item should be visible via DTE.
                return CreateProjectItemTreeNode();
            }
            else
            {
                return CreateProjectTreeNode();
            }

            IProjectTree UpdateTreeNode()
            {
                return node.SetProperties(
                    caption: caption,
                    browseObjectProperties: browseObjectProperties,
                    icon: icon,
                    flags: flags);
            }

            IProjectItemTree CreateProjectItemTreeNode()
            {
                var itemContext = ProjectPropertiesContext.GetContext(
                    _unconfiguredProject,
                    file: filePath,
                    itemType: schemaItemType,
                    itemName: filePath);

                return TreeConstruction.NewTree(
                    caption: caption,
                    item: itemContext,
                    propertySheet: null,
                    browseObjectProperties: browseObjectProperties,
                    icon: icon,
                    expandedIcon: null,
                    visible: true,
                    flags: flags);
            }

            IProjectTree CreateProjectTreeNode()
            {
                return TreeConstruction.NewTree(
                    caption: caption,
                    filePath: filePath,
                    browseObjectProperties: browseObjectProperties,
                    icon: icon,
                    visible: true,
                    flags: flags);
            }

            void FilterProperties()
            {
                if (_projectTreePropertiesProviders.Count == 0)
                {
                    return;
                }

                var updatedNodeParentContext = new ProjectTreeCustomizablePropertyContext(
                    parentNodeFlags: parentNode?.Flags ?? default);

                var updatedValues = new ReferencesProjectTreeCustomizablePropertyValues
                {
                    Caption = caption,
                    Flags = flags,
                    Icon = icon,
                };

                foreach (Lazy<IProjectTreePropertiesProvider, IOrderPrecedenceMetadataView> provider in _projectTreePropertiesProviders)
                {
                    provider.Value.CalculatePropertyValues(updatedNodeParentContext, updatedValues);
                }

                caption = updatedValues.Caption;
                icon = updatedValues.Icon;
                flags = updatedValues.Flags;
            }
        }
    }

    /// <summary>
    /// A private implementation of <see cref="IProjectTreeCustomizablePropertyContext"/> used when creating or updating
    /// dependencies nodes.
    /// </summary>
    private sealed class ProjectTreeCustomizablePropertyContext : IProjectTreeCustomizablePropertyContext
    {
        public static readonly ProjectTreeCustomizablePropertyContext Default = new(ProjectTreeFlags.Empty);

        public ProjectTreeCustomizablePropertyContext(ProjectTreeFlags parentNodeFlags) => ParentNodeFlags = parentNodeFlags;

        public string ItemName => string.Empty;
        public string? ItemType => null;
        public IImmutableDictionary<string, string> Metadata => ImmutableDictionary<string, string>.Empty;
        public ProjectTreeFlags ParentNodeFlags { get; }
        public bool ExistsOnDisk => false;
        public bool IsFolder => false;
        public bool IsNonFileSystemProjectItem => true;
        public IImmutableDictionary<string, string> ProjectTreeSettings => ImmutableDictionary<string, string>.Empty;
    }
}
