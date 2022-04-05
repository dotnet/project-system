This document describes, at a high level, the design and implementation of the Dependencies node with pointers to the important types.

There are two fundamentally different types of dependency for our purposes, and they are handled separately:

||Description|Populated|Displayed via
|-|:-|:-|:-|
|[Top-level dependencies](#top-level-dependencies)|Direct dependencies of the project. A single level deep.|Eagerly|CPS's APIs|
|[Transitive dependencies](#transitive-dependencies)|Dependencies brought by top level dependencies. Arbitrarily deep.|Lazily|Solution Explorer's APIs|

---

# Top-level dependencies

## Overview

Top level dependencies are sourced in two ways in this repo:

1. We use MSBuild project evaluation and design-time build to source data about most kinds of dependencies.

   |Type|Unresolved item type|Resolved item type|Comment|
   |:-|:-|:-|:-|
   |Analyzer|`AnalyzerReference`|`ResolvedAnalyzerReference`|Roslyn analyzers|
   |Assembly|`Reference`|`ResolvedReference`|DLLs not sourced directly from disk|
   |COM|`COMReference`|`ResolvedCOMReference`||
   |Framework|`FrameworkReference`|`ResolvedFrameworkReference`|For .NET Core 2.0+ projects|
   |Package|`PackageReference`|`ResolvedPackageReference`|NuGet|
   |Project|`ProjectReference`|`ResolveProjectReference`||
   |SDK|`SDKReference`|`ResolvedSDKReference`|For extension SDKs, not the .NET SDK|

2. We have a dedicated pathway for shared projects, which pulls data from CPS dataflow.

Additionally, third parties may provide extensions (see [Extensibily model](#extensibility-model) for more information):

1. The WebTools have their own provider for NPM dependencies.

## Dependencies sourced via MSBuild items

This diagram gives an insight into the flow of data through the dependencies tree subsystem for top-level dependencies obtained via MSBuild.

```mermaid
flowchart LR
  subgraph UnconfiguredProject Scope
    handlers[IDependenciesRuleHandler]
    subgraph ConfiguredProject Scope
      direction TB
      subgraph CPS Data Sources
        evaluation([Evaluation Data])
        design-time-build([Design-time Build Data])
      end
    end
    evaluation == "snapshot + delta" ==> DependencyRulesSubscriber
    design-time-build == "snapshot + delta" ==> DependencyRulesSubscriber
    handlers -- "import many" -.-> DependencyRulesSubscriber
    DependencyRulesSubscriber -- "IDependenciesChanges (delta)" --> DependenciesSnapshotProvider
    IDependenciesTreeViewProvider["IDependenciesTreeViewProvider.BuildTreeAsync"]
    DependenciesSnapshotProvider == DependenciesSnapshot ==> IDependenciesTreeViewProvider
    IDependenciesTreeViewProvider -- IProjectTree --> ProjectTreeProviderBase.SubmitTreeUpdateAsync
  end

  click handlers "https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/CrossTarget/IDependenciesRuleHandler.cs" "Click to view source"
  click DependencyRulesSubscriber "https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/DependencyRulesSubscriber.cs" "Click to view source"
  click IDependenciesTreeViewProvider "https://github.com/dotnet/project-system/blob/main/src/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/IDependenciesTreeViewProvider.cs" "Click to view source"
  click DependenciesSnapshotProvider "https://github.com/dotnet/project-system/blob/main/src/src/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/DependenciesSnapshotProvider.cs" "Click to view source"
```

Bold lines indicate Dataflow subscriptions.

## The CPS view of top-level dependencies

On the CPS side the project tree is composed of instances of the `IProjectTree` and `IProjectItemTree` interfaces. `IProjectTree` captures the structure of the tree (e.g. it has properties to access the parent and child nodes) and the "UI" aspects of the node&mdash;name, icons, visibility, etc. An `IProjectItemTree` captures all of that but also represents a concrete item within the project like a file, assembly reference, or NuGet package.

An `IProjectTree` is immutable. When a part of the tree needs to be updated, we need to replace it and form a new tree.

Components wishing to add items to the project tree must implement and export the `IProjectTreeProvider` interface. In practice we implement the interface by deriving from the abstract `ProjectTreeProviderBase` class provided by CPS. In general, we receive events/messages from CPS detailing changes to the project, generate an updated `IProjectTree`, and then pass it back to CPS via `ProjectTreeProviderBase.SubmitTreeUpdateAsync`.

## The .NET Project System view of top-level dependencies

Internally every top-level dependency is represented as an [`IDependency`][IDependency].

All [`IDependency`][IDependency]s for a given target framework in a given project are collected together into a [`TargetedDependenciesSnapshot`][TargetedDependenciesSnapshot]. All of those for a given project are, in turn, collected into a [`DependenciesSnapshot`][DependenciesSnapshot].

The [`DependenciesSnapshotProvider`][DependenciesSnapshotProvider] is responsible for providing access to the current [`DependenciesSnapshot`][DependenciesSnapshot] and firing events when the snapshot has changed.

Much of the code for the Dependencies node is concerned with creating [`IDependency`][IDependency]s and translating them into new `IProjectTree`s when they change.

## CPS/Project System interaction

Top-level dependencies (e.g., `Reference`, `PackageReference`, `ProjectReference`, and `Analyzer` items in the project file) are represented with `IProjectTree` nodes. This makes it possible for them to be represented in the project's `IVsHierarchy` and, crucially, makes it easier to code the sorts of interactions users expect for these items. For example, a user should be able to right-click on an assembly reference and remove it, or modify the properties of an assembly reference.

> Aside: There are exceptions to this. In practice analyzers are not directly referenced but rather brought in as part of a NuGet package. We still represent all analyzers as `IProjectTree` items directly under the "Analyzers" node. This makes it much easier for the C#/VB language service to add nodes for each diagnostic underneath the analyzer's node, and it makes it easy for the user to find so they can check the severity of the diagnostic and potentially change it using the context menu.

### DependenciesProjectTreeProvider

The primary connection point between CPS and the Dependencies node is the [`DependenciesProjectTreeProvider`][DependenciesProjectTreeProvider], implementing the `IProjectTreeProvider` interface. It is directly responsible for the following:

1. Creating the `IProjectTree` for the "Dependencies" node itself (children are handled elsewhere).
2. Handling explicit commands to copy or remove a node underneath the "Dependencies" node.
3. Mapping back and forth between `IProjectTree` instances and paths.
4. Listening for changes to the set of dependencies via the [`DependenciesSnapshotProvider`][DependenciesSnapshotProvider], delegating to the [`IDependenciesTreeViewProvider`][IDependenciesTreeViewProvider] to rebuild the tree underneath the Dependencies node, and submitting the updated tree back to CPS.

### Generating dependencies

As evaluations and design-time builds occur, CPS pushes project changes through TPL Dataflow blocks (available via `IProjectSubscriptionService`, where `ProjectRuleSource` provides evaluation data, and `JointRuleSource` provides design-time build data).

Various implementations of [`IDependenciesRuleHandler`][IDependenciesRuleHandler] exist, and each specifies the set of rules they wish to handle (e.g. `PackageReference`, `ResolvedProjectReference`, etc.). The abstract class [`DependenciesRuleHandlerBase`][DependenciesRuleHandlerBase] exists to make implementing [`IDependenciesRuleHandler`][IDependenciesRuleHandler] easier.

The [`DependencyRulesSubscriber`][DependencyRulesSubscriber] (implementing [`IDependencyCrossTargetSubscriber`][IDependencyCrossTargetSubscriber]) subscribes via Dataflow to the union of rules specified by the handlers. When updates are received each handler is given a chance to add/update/remove [`IDependencyModel`][IDependencyModel] instances through the builder. Once complete, the `IDependencyCrossTargetSubscriber.DependenciesChanged` event is fired, carrying dependency model changes.

Each project has an instance of [`DependenciesSnapshotProvider`][DependenciesSnapshotProvider] that holds the latest `DependenciesSnapshot` object. It imports `IDependencyCrossTargetSubscriber` implementations (such as `DependencyRulesSubscriber`) and subscribes to their `DependenciesChanged` events. When these events fire, the current snapshot is combined with changes to produce a new snapshot. That snapshot is then propagated via the `DependenciesSnapshotProvider.SnapshotChanged` event.

This `SnapshotChanged` event is then handled by [`DependenciesProjectTreeProvider`][DependenciesProjectTreeProvider] to update the tree.

### Translating snapshots to trees

Most of the work of translating [`IDependency`][IDependency]s to `IProjectTree`s is done by [`DependenciesTreeViewProvider`][DependenciesTreeViewProvider] (implementing [`IDependenciesTreeViewProvider`][IDependenciesTreeViewProvider]). It takes a [`DependenciesSnapshot`][DependenciesSnapshot] and generates the nodes for the target frameworks, the groupings under each framework, (Assemblies, Analyzers, Packages, Projects, etc.), and the top-level nodes under each of those groupings. In the common case that a project has a single target framework it leaves out the framework node entirely and simply hangs the different groupings directly off the `IProjectTree` for the Dependencies node.

The [`DependenciesTreeViewProvider`][DependenciesTreeViewProvider] traverses down the existing `IProjectTree` and the new [`DependenciesSnapshot`][DependenciesSnapshot] in parallel, starting from the Dependencies node itself and proceeding on to target framework, groupings, and then the individual top-level dependencies. Along the way it incrementally generates new `IProjectTree`s as it finds dependencies that have been updated, added, or removed.

[`IDependency`][IDependency]s are not translated directly into `IProjectTree`s. They are first converted to [`IDependencyViewModel`][IDependencyViewModel]s and those in turn become the `IProjectTree`s. This makes it a little easier to create the `IProjectTree`s for targets and groups (e.g. the Assemblies, NuGet, Projects, etc. nodes) which are not themselves [`IDependency`][IDependency]s. In some cases a [`IDependencyModel`][IDependencyModel] may be converted directly to a [`IDependencyViewModel`][IDependencyViewModel].

### Identifiers

#### `IDependencyModel` identifiers

Instances of [`IDependencyModel`][IDependencyModel]s produced by an [`IProjectDependenciesSubTreeProvider`][IProjectDependenciesSubTreeProvider] must have an `Id` propety that's unique to that provider and that project.

For dependencies obtained via MSBuild evaluations (Packages, Assemblies, etc...) the `Id` is just the `OriginalItemSpec`.

#### `IDependency` identifiers

Once a dependency model is integrated into a dependencies snapshot as an [`IDependency`][IDependency], its `Id` will be constructed from the target framework, provider type and model ID. For example: `netstandard2.0/nugetdependency/newtonsoft.json`

This allows the ID to be unique within both the provider and the target framework.

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

# Extensibility model

Project flavors can extend the Dependencies node with additional sub-trees. To do so:

- Implement and export an [`IProjectDependenciesSubTreeProvider`][IProjectDependenciesSubTreeProvider] implementation per sub-tree
- Provide a custom implementation of [`IDependencyModel`][IDependencyModel]
- Potentially implement `IProjectTreeProvider` (usually by deriving from `ProjectTreeProviderBase`)

The _Web Tools Extensions_ project is a good example of a project flavor that does this.


[IDependenciesRuleHandler]:               /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/CrossTarget/IDependenciesRuleHandler.cs "IDependenciesRuleHandler.cs"
[DependenciesProjectTreeProvider]:        /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/DependenciesProjectTreeProvider.cs "DependenciesProjectTreeProvider.cs"
[DependenciesTreeViewProvider]:           /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/DependenciesTreeViewProvider.cs "DependenciesTreeViewProvider.cs"
[IDependencyModel]:                       /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/IDependencyModel.cs "IDependencyModel.cs"
[IDependenciesTreeViewProvider]:          /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/IDependenciesTreeViewProvider.cs "IDependenciesTreeViewProvider.cs"
[IProjectDependenciesSubTreeProvider]:    /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/IProjectDependenciesSubTreeProvider.cs "IProjectDependenciesSubTreeProvider.cs"
[IDependencyViewModel]:                   /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Models/IDependencyViewModel.cs "IDependencyViewModel.cs"
[DependenciesSnapshot]:                   /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Snapshot/DependenciesSnapshot.cs "DependenciesSnapshot.cs"
[IDependency]:                            /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Snapshot/IDependency.cs "IDependency.cs"
[TargetedDependenciesSnapshot]:           /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Snapshot/TargetedDependenciesSnapshot.cs "TargetedDependenciesSnapshot.cs"
[DependenciesRuleHandlerBase]:            /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/RuleHandlers/DependenciesRuleHandlerBase.cs "DependenciesRuleHandlerBase.cs"
[DependencyRulesSubscriber]:              /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/DependencyRulesSubscriber.cs "DependencyRulesSubscriber.cs"
[DependenciesSnapshotProvider]:           /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/DependenciesSnapshotProvider.cs "DependenciesSnapshotProvider.cs"
[IDependencyCrossTargetSubscriber]:       /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/IDependencyCrossTargetSubscriber.cs "IDependencyCrossTargetSubscriber.cs"
[ProjectRuleHandler]:                     /src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Tree/Dependencies/Subscriptions/ProjectRuleHandler.cs "ProjectRuleHandler.cs"

[IRelation]:                                   /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/IRelation.cs "IRelation.cs"
[IRelatableItem]:                              /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/IRelatableItem.cs "IRelatableItem.cs"
[IDependenciesTreeProjectSearchContext]:       /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/IDependenciesTreeProjectSearchContext.cs "IDependenciesTreeProjectSearchContext.cs"
[IDependenciesTreeConfiguredProjectSearchContext]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/IDependenciesTreeConfiguredProjectSearchContext.cs "IDependenciesTreeConfiguredProjectSearchContext.cs"
[RelatableItemBase]:                           /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/RelatableItemBase.cs "RelatableItemBase.cs"
[AttachedCollectionItemBase]:                  /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/AttachedCollectionItemBase.cs "AttachedCollectionItemBase.cs"
[RelationBase]:                                /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/RelationBase.cs "RelationBase.cs"
[IDependenciesTreeSearchProvider]:             /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/AttachedCollections/IDependenciesTreeSearchProvider.cs "IDependenciesTreeSearchProvider.cs"
