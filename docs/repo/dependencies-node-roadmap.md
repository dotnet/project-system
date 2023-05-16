This document describes, at a high level, the design and implementation of the Dependencies node with pointers to the important types.

There are two fundamentally different types of dependency for our purposes, and they are handled separately:

|                                                     | Description                                                       | Populated | Displayed via            |
|-----------------------------------------------------|:------------------------------------------------------------------|:----------|:-------------------------|
| [Top-level dependencies](#top-level-dependencies)   | Direct dependencies of the project. A single level deep.          | Eagerly   | CPS's APIs               |
| [Transitive dependencies](#transitive-dependencies) | Dependencies brought by top level dependencies. Arbitrarily deep. | Lazily    | Solution Explorer's APIs |

---

# Top-level dependencies

## Modelling a top-level dependency

Each top-level dependency in the tree is modelled by an instance of [`IDependency`][IDependency]. These objects are immutable snapshots of the state of a dependency at a point in time.

The interface has the following properties:

- `Id` &mdash; Uniquely identifies the dependency within its group (and slice, for configured dependencies, described below).
- `Caption` &mdash; Friendly name of the dependency, for use in the UI.
- `Icon` &mdash; The icon to display in Solution Explorer.
- `Flags` &mdash; The set of `ProjectTreeFlags` applicable to this dependency.
- `DiagnosticLevel` &mdash; The severity level of any diagnostic associated with this dependency.

If a dependency populates data in the _Properties_ window of VS, then it must also implement [`IDependencyWithBrowseObject`][IDependencyWithBrowseObject], which has the following properties:

- `UseResolvedReferenceRule` &mdash; Whether the browse object for this dependency represents a resolved reference.
- `FilePath` &mdash; The resolved path of the dependency, where appropriate.
- `SchemaName` &mdash; The name of the rule (also known as the schema name) that backs this dependency's browse object.
- `SchemaItemType` &mdash; The name of MSBuild item type (where relevant) that backs this dependency's browse object.
- `BrowseObjectProperties` &mdash; The names and values of properties to use in the browse object.

## Providing top-level dependencies

There are two varieties of top-level dependency:

1. **Configured dependencies** which come from a specific project configuration, such as `Debug|AnyCPU` (or `Debug|AnyCPU|net8.0` if multi-targeting). These dependencies are most likely sourced from MSBuild, where the concept of configuration is embedded.

2. **Unconfigured dependencies** which exist outside of any specific configuration. For example JavaScript/TypeScript projects might use NPM packages, and NPM has no concept of MSBuild configurations.

Snapshots of dependencies are produced via Dataflow blocks, which are then aggregated to produce a single snapshot, from which the top-level nodes in the tree are populated.

The .NET Project System exports MEF components that contribute to the dependencies snapshot. Third parties may do the same, and have their dependencies included in the tree. See [extensibility](#extensibility) for more information on adding other types of dependencies to the tree.

## Configured dependencies

Configured dependencies are provided by exports of [`IDependencySliceSubscriber`][IDependencySliceSubscriber].

These components may subscribe to project data within a given project configuration "slice". The tree always shows data from the active configuration, and slices hide that complexity from consuming code, making things much simpler than they would otherwise be.

### Configured dependencies from MSBuild data

Within the .NET Project System, we export an implementation of [`IDependencySliceSubscriber`][IDependencySliceSubscriber] via the class [`MSBuildDependencySubscriber`][MSBuildDependencySubscriber]. This class produces snapshots of dependencies within a project configuration slice, where those dependencies are sourced from MSBuild items.

Each dependency has a corresponding unresolved (from evaluation) and resolved (from build) MSBuild item. Dependencies coming from MSBuild items are obtained during evaluation, however at that point we do not know whether the claimed dependency is valid and able to be resolved. During the design-time build, dependencies are checked for validity, such as whether they exist and are compatible with the project. If so, they are _resolved_ and a resolved item is produced in the build results. [`MSBuildDependencySubscriber`][MSBuildDependencySubscriber] observes both the unresolved and resolved MSBuild items to determine the state of the dependency.

The set of items supported by [`MSBuildDependencySubscriber`][MSBuildDependencySubscriber] is defined by [`IMSBuildDependencyFactory`][IMSBuildDependencyFactory] exports, and the NET Project System ships factory implementations for the following kinds of MSBuild dependencies:

| Type      | Unresolved item type | Resolved item type           | Comment                              |
|:----------|:---------------------|:-----------------------------|:-------------------------------------|
| Analyzer  | `AnalyzerReference`  | `ResolvedAnalyzerReference`  | Roslyn analyzers                     |
| Assembly  | `Reference`          | `ResolvedReference`          | DLLs not sourced directly from disk  |
| COM       | `COMReference`       | `ResolvedCOMReference`       |                                      |
| Framework | `FrameworkReference` | `ResolvedFrameworkReference` | For .NET Core 2.0+ projects          |
| Package   | `PackageReference`   | `ResolvedPackageReference`   | NuGet packages                       |
| Project   | `ProjectReference`   | `ResolveProjectReference`    |                                      |
| SDK       | `SDKReference`       | `ResolvedSDKReference`       | For extension SDKs, not the .NET SDK |

[`MSBuildDependencySubscriber`][MSBuildDependencySubscriber] takes the set of [`IMSBuildDependencyFactory`][IMSBuildDependencyFactory] exports and subscribes to both evaluation (via `ProjectRuleSource`) and build data (via `JointRuleSource`, which also includes evaluation) for the item types they advertise. As Dataflow pushes updates through those data sources for those items, the [`IMSBuildDependencyFactory`][IMSBuildDependencyFactory] exports are used to construct and update the set of `IDependency` objects as necessary.

The abstract class [`MSBuildDependencyFactoryBase`][MSBuildDependencyFactoryBase] exists to make implementing [`IMSBuildDependencyFactory`][IMSBuildDependencyFactory] easier.

### Configured shared project dependencies

Shared projects are not exposed via MSBuild items. CPS provides a separate data source for these items, which we subscribe to in [`SharedProjectDependencySubscriber`][SharedProjectDependencySubscriber].

## Unconfigured dependencies

Dependencies that exist outside of any particular project configuration are provided by exports of [`IDependencySubscriber`][IDependencySubscriber].

The .NET Project System does not currently provide any dependencies via this mechanism, however other project types may use this mechanism to do so. For example, JavaScript/TypeScript projects provide NPM package dependencies, which have no concept of MSBuild project configurations.

## Aggregating dependency snapshots

This diagram outlines how the various MEF exports and Dataflow blocks come together to produce the overall snapshot.

```mermaid
flowchart TB
    subgraph Per Slice
        direction TB
        evaluation([Evaluation Data])
        design-time-build([Design-time Build Data])
    evaluation == "snapshot + delta" ==> MSBuildDependencySubscriber
    design-time-build == "snapshot + delta" ==> MSBuildDependencySubscriber
    IMSBuildDependencyFactory -- "import many" -.-> MSBuildDependencySubscriber
    MSBuildDependencySubscriber -- "implements" -.-> IDependencySliceSubscriber
    SharedProjectDependencySubscriber -- "implements" -.-> IDependencySliceSubscriber
    end
    IDependencySliceSubscriber -- "import many" -.-> DependenciesSnapshotProvider
    IDependencySubscriber -- "import many" -.-> DependenciesSnapshotProvider
    DependenciesTreeBuilder["DependenciesTreeBuilder.BuildTreeAsync"]
    DependenciesSnapshotProvider == DependenciesSnapshot ==> DependenciesTreeBuilder
    DependenciesTreeBuilder -- IProjectTree --> ProjectTreeProviderBase.SubmitTreeUpdateAsync
    LegacyDependencySubscriber -- "implements" -.-> IDependencySubscriber
    IProjectDependenciesSubTreeProvider -- "import many" -.-> LegacyDependencySubscriber

    click IMSBuildDependencyFactory "https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/IMSBuildDependencyFactory.cs" "Click to view source"
    click MSBuildDependencySubscriber "https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/MSBuildDependencies/MSBuildDependencySubscriber.cs" "Click to view source"
    click DependenciesTreeBuilder "https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/DependenciesTreeBuilder.cs" "Click to view source"
    click DependenciesSnapshotProvider "https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/DependenciesSnapshotProvider.cs" "Click to view source"
    click IDependencySubscriber "https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/IDependencySubscriber.cs" "Click to view source"
    click IDependencySliceSubscriber "https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/IDependencySliceSubscriber.cs" "Click to view source"
    click SharedProjectDependencySubscriber "https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/SharedProjectDependencySubscriber.cs" "Click to view source"
```

Bold lines indicate Dataflow subscriptions.

All [`IDependency`][IDependency]s for a given slice in a given project are collected together into a [`DependenciesSnapshotSlice`][DependenciesSnapshotSlice].

All of those for a given project are, in turn, collected into a [`DependenciesSnapshot`][DependenciesSnapshot].

The [`DependenciesSnapshotProvider`][DependenciesSnapshotProvider] is responsible for providing access to the current [`DependenciesSnapshot`][DependenciesSnapshot] and publishing updates when the snapshot changes.

## Constructing the tree

[`DependenciesTreeProvider`][DependenciesTreeProvider] coordinates the building of the tree. It:

- Subscribes to [`DependenciesSnapshot`][DependenciesSnapshot] updates from [`DependenciesSnapshotProvider`][DependenciesSnapshotProvider].
- Uses [`DependenciesTreeBuilder`][DependenciesTreeBuilder] to walks the existing tree and the snapshot, updating the tree where necessary.
- Calls into its base method `ProjectTreeProviderBase.SubmitTreeUpdateAsync` when we want to apply changes to the project tree in Solution Explorer.

The tree itself is represented via `IProjectTree` and `IProjectItemTree` instances, which themselves form an immutable tree. Our provider submits updates to CPS which are them merged ("root grafted" in CPS terminology) into the project tree.

- `IProjectTree` captures the structure of the tree (e.g. it has properties to access the parent and child nodes) and the "UI" aspects of the node &mdash; name, icons, visibility, etc.
- `IProjectItemTree` captures everything that `IProjectTree` does but also represents a concrete item within the project like a file, assembly reference, or NuGet package. Importantly, these objects are exposed via DTE.

Note that for multi-targeting projects, only dependencies within the "primary" configuration (usually the first) are exposed via DTE. DTE predates the concept of multi-targeting and lacks the ability to express multiple implicitly active configurations.

CPS uses the project tree to construct the `IVsHierarchy` view of the project that is consumed by many other components and extensions within VS. This is a standard way of representing data about the project. For more information see the [`IVsHierarchy` interface documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivshierarchy?view=visualstudiosdk-2022#remarks), however this is not specific to the dependencies tree.

## Tree operations

In addition to constructing the tree, [`DependenciesTreeProvider`][DependenciesTreeProvider] is responsible for some additional operations on the tree:

- Handling explicit commands to copy or remove a node underneath the "Dependencies" node.
- Mapping back and forth between `IProjectTree` instances and paths.

---

# Transitive dependencies

## Overview

Tree nodes for transitive dependencies appear underneath top-level dependencies. They are populated lazily as the user expands the tree, or when the user searches in Solution Explorer.

The above snapshots only contain top-level dependencies. In practice, these represent a small percentage of the total number of items in a project's dependency graph. For performance reasons, we defer obtaining and retaining data about transitive dependencies until the user expands enough of Solution Explorer for them to become visible, or performs a search in Solution Explorer.

Transitive dependencies are 'attached' to the tree via a series of APIs we'll call `IAttachedCollection` APIs. These are the low-level APIs upon which view of hierarchy items is implemented (along with the less common graph nodes, a.k.a. progression nodes). These allow lazily populating tree items and provide a lot of control over presentation and interaction patterns for these nodes.

However implementing `IAttachedCollection` APIs is quite involved, especially if you want to support Solution Explorer search.

The Project System provides a higher-level system for building the transitive dependencies graph. There are two key concepts, _Items_ and _Relations_. Let's look at each in turn.

## Items

Items are modelled via [`IRelatableItem`][IRelatableItem]. Each kind of item in a project's dependency tree has its own item type.

The easiest way to define an item is to derive from [`RelatableItemBase`][RelatableItemBase] which in turn derives from [`AttachedCollectionItemBase`][AttachedCollectionItemBase].

- [`RelatableItemBase`][RelatableItemBase] makes it easier to implement [`IRelatableItem`][IRelatableItem] correctly, implementing much of the required protocol and exposing abstract/virtual members for customising behaviour.

- [`AttachedCollectionItemBase`][AttachedCollectionItemBase] exposes presentation and interaction members for the item such as its `Text`, icons (`IconMoniker`, `ExpandedIconMoniker`, `OverlayIconMoniker`, `StateIconMoniker`), `FontStyle`, `FontWeight`, `ToolTipText`, browse objects (for Visual Studio's _Properties_ pane) and so forth. Derivations may expose additional patterns such as `IInvocationPattern` for double-click logic, `IContextMenuPattern` for context menus, `IDragDropSourcePattern`/`IDragDropTargetPattern` for drag/drop, and so on.

A typical item implementation will derive from [`AttachedCollectionItemBase`][AttachedCollectionItemBase], define properties for the item's state, and override presentation members to specify icons, browse objects and so forth.

Item state is important as we will see when we talk about relations.

## Relations

Relations are modelled via [`IRelation`][IRelation] and represent bi-directional linkages between items in the tree.

The easiest way to define a relation is to derive from [`RelationBase<TParent, TChild>`][RelationBase]. This offers an extra level of type safety over [`IRelation`][IRelation].

A relation may be between two different kinds of item (e.g. a _package_ contains a _compile-time assembly_) or even two items of the same kind (e.g. a _package_ contains a reference to another _package_).

A relation that understands an item can contribute to the collection of its children or parents. Such a relation is also used to update the collection of materialized children whenever a parent's state is modified (which can be applied recursively down the tree).

When a relation is computing the children or parents of an item it will require adequate state to be available on the source item. This will factor into the design of the items and their backing data. Relations are MEF exports, so they may import additional parts for use in their implementation.

When the user expands Dependencies tree items in Solution Explorer, relations are used to lazily produce the children to show beneath that item.

Use of relations to determine an item's parents comes into play when searching, which we will look at next.

## Search

When the user performs a search within Solution Explorer, it is unlikly that the entire dependency graph has been materialised. Therefore, in order to search we need an additional mechanism that doesn't rely on materialised items.

Extensions that add transitive dependencies to the tree via the above items/relations should also implement [`IDependenciesTreeSearchProvider`][IDependenciesTreeSearchProvider] to support search in Solution Explorer. The implementation should look at the underlying data source and produce any items that match the user's search.

Each search operation runs with an [`IDependenciesTreeProjectSearchContext`][IDependenciesTreeProjectSearchContext] which exposes a `CancellationToken`, the `UnconfiguredProject`, and allows creating a per-target [`IDependenciesTreeConfiguredProjectSearchContext`][IDependenciesTreeConfiguredProjectSearchContext] via which results may be submitted.

Implementations should check for cancellation periodically, as a modification to the user's search string or reaching the maximum number of results will both result in cancellation.

As hinted above, relations are used to construct the lineage of ancestor items for any submitted search result items. This is the reason that relations must be bi-directional. The ancestral lineage must connect at some point with a top-level dependency's `IVsHierarchyItem` in order for the search result to be visible. This will happen automatically via `IRelatableItem.TryGetProjectNode`. If an extension provides top-level dependencies, it should override `RelatableItemBase.TryGetProjectNode` on the corresponding item type to provide this connection.

## Example

See `FrameworkReferenceItem`, `FrameworkReferenceAssemblyItem` and `FrameworkToFrameworkAssemblyRelation` for an example of how transitive references beneath Framework reference items are populated. In this case, we display the assemblies that make up the framework.

Most other implementations exist in the NuGet.Client repo on GitHub.

---

# Extensibility

⚠️ NOTE in 17.7 the dependencies tree code was largely rewritten. Extensibility was a key consideration of that rewrite, however for the time being many of the types discussed above are `internal`. We are working to refine the API before making portions of it public for extension. Until then, extenders may continue to use the legacy extensibility API, which remains supported.

TODO document the APIs needed to provide custom top-level dependencies, and provide examples.

## Legacy extensibility API

Project extensions may extend the _Dependencies_ node with additional sub-trees. To do so:

- Implement and export an [`IProjectDependenciesSubTreeProvider`][IProjectDependenciesSubTreeProvider] implementation per sub-tree.
- Provide a custom implementation of [`IDependencyModel`][IDependencyModel].

The _Web Tools Extensions_ project is a good example of a project flavor that does this.

Note that this API will only provide top-level dependencies. To add descendent nodes beneath these top-level dependencies, see [transitive dependencies](#transitive-dependencies).

## Sibling tree

Alternatively, you could provide your own sibling alongside the _Dependencies_ node by implementing your own `IProjectTreeProvider` "root graft" (usually by deriving from `ProjectTreeProviderBase`). This would be a lot more work and would potentially provide a more confusing experience for users. But it is an option that may make sense in your scenario.


[DependenciesTreeBuilder]:                ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/DependenciesTreeBuilder.cs "DependenciesTreeBuilder.cs"
[DependenciesTreeProvider]:               ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/DependenciesTreeProvider.cs "DependenciesTreeProvider.cs"

[IDependencyModel]:                       ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/IDependencyModel.cs "IDependencyModel.cs"
[IProjectDependenciesSubTreeProvider]:    ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/IProjectDependenciesSubTreeProvider.cs "IProjectDependenciesSubTreeProvider.cs"

[DependenciesSnapshot]:                   ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Snapshot/DependenciesSnapshot.cs "DependenciesSnapshot.cs"
[DependenciesSnapshotSlice]:              ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Snapshot/DependenciesSnapshotSlice.cs "DependenciesSnapshotSlice.cs"
[IDependency]:                            ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Snapshot/IDependency.cs "IDependency.cs"
[IDependencyWithBrowseObject]:            ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Snapshot/IDependencyWithBrowseObject.cs "IDependencyWithBrowseObject.cs"

[IMSBuildDependencyFactory]:              ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/MSBuildDependencies/IMSBuildDependencyFactory.cs "IMSBuildDependencyFactory.cs"
[MSBuildDependencyFactoryBase]:           ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/MSBuildDependencies/MSBuildDependencyFactoryBase.cs "MSBuildDependencyFactoryBase.cs"
[MSBuildDependencySubscriber]:            ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/MSBuildDependencies/MSBuildDependencySubscriber.cs "MSBuildDependencySubscriber.cs"
[DependenciesSnapshotProvider]:           ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/DependenciesSnapshotProvider.cs "DependenciesSnapshotProvider.cs"
[IDependencySubscriber]:                  ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/IDependencySubscriber.cs "IDependencySubscriber.cs"
[IDependencySliceSubscriber]:             ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/IDependencySliceSubscriber.cs "IDependencySliceSubscriber.cs"
[SharedProjectDependencySubscriber]:      ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/SharedProjectDependencySubscriber.cs "SharedProjectDependencySubscriber.cs"
[DependenciesSnapshotProvider]:           ../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/DependenciesSnapshotProvider.cs "DependenciesSnapshotProvider.cs"

[IRelation]:                                       ../../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/IRelation.cs "IRelation.cs"
[IRelatableItem]:                                  ../../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/IRelatableItem.cs "IRelatableItem.cs"
[IDependenciesTreeProjectSearchContext]:           ../../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/IDependenciesTreeProjectSearchContext.cs "IDependenciesTreeProjectSearchContext.cs"
[IDependenciesTreeConfiguredProjectSearchContext]: ../../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/IDependenciesTreeConfiguredProjectSearchContext.cs "IDependenciesTreeConfiguredProjectSearchContext.cs"
[RelatableItemBase]:                               ../../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/RelatableItemBase.cs "RelatableItemBase.cs"
[AttachedCollectionItemBase]:                      ../../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/AttachedCollectionItemBase.cs "AttachedCollectionItemBase.cs"
[RelationBase]:                                    ../../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/RelationBase.cs "RelationBase.cs"
[IDependenciesTreeSearchProvider]:                 ../../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/IDependenciesTreeSearchProvider.cs "IDependenciesTreeSearchProvider.cs"
