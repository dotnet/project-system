# Dependencies Node Roadmap

This document describes, at a high level, the design and implementation of the Dependencies node with pointers to the important types.

Let's start at the top and work our way down. There are two fundamentally different paths that dependency information takes from the Project System to Solution Explorer. Direct dependencies go through CPS, whereas transitive dependencies generally go through graph node providers.

## Overview

### The CPS View of Dependencies

On the CPS side the project tree is composed of instances of the `IProjectTree` and `IProjectItemTree` interfaces. `IProjectTree` captures the structure of the tree (e.g. it has properties to access the parent and child nodes) and the "UI" aspects of the node&mdash;name, icons, visibility, etc. An `IProjectItemTree` captures all of that but also represents a concrete item within the project like a file, assembly reference, or NuGet package.

An `IProjectTree` is immutable. When a part of the tree needs to be updated, we need to replace it and form a new tree.

Components wishing to add items to the project tree must implement and export the `IProjectTreeProvider` interface. In practice we implement the interface by deriving from the abstract `ProjectTreeProviderBase` class provided by CPS. In general, we receive events/messages from CPS detailing changes to the project, generate an updated `IProjectTree`, and then pass it back to CPS via `ProjectTreeProviderBase.SubmitTreeUpdateAsync`.

### The Graph Node Provider View of Dependencies

On the Graph Node side, transitive dependencies are represented as `GraphNode` instances and are frequently associated with `IGraphContexts`. In contrast to `IProjectTree`s, graphs are not realized in their entirety "up front" nor are they immutable. Instead, a graph starts with a small set of initial nodes. As the user expands nodes or searches within the graph an `IGraphProvider` implementation is asked to mutate the graph via an `IGraphContext`. The `IGraphContext` contains the current graph, a description of the operation at hand (checking if a node has children, getting the set of children, search for nodes matching certain text, etc.) and a set of "input" nodes marking the starting point for the operation. The `IGraphProvider` then adds `GraphNodes` to the graph as appropriate.

### The Project System View of Dependencies

Internally every individual dependency (both direct and transitive) is represented as an [`IDependency`][IDependency].

All the [`IDependency`][IDependency]s for a given target framework in a given project are collected together into a [`TargetedDependenciesSnapshot`][TargetedDependenciesSnapshot]. All of those for a given project are, in turn, collected into a [`DependenciesSnapshot`][DependenciesSnapshot].

The [`DependenciesSnapshotProvider`][DependenciesSnapshotProvider] is responsible for providing access to the current [`DependenciesSnapshot`][DependenciesSnapshot] and firing events when the snapshot has changed.

Much of the code for the Dependencies node is concerned with creating [`IDependency`][IDependency]s and translating them into new `IProjectTree`s when they change.

## CPS/Project System Interaction

In general, items _directly_ referenced by the project file (e.g., through `Reference`, `ProjectReference`, and `Analyzer` items in the project file) are represented with `IProjectTree` nodes as opposed to `GraphNodes`. This makes it possible for them to be represented in the project's `IVsHierarchy` and, crucially, makes it easier to code the sorts of interactions users expect for these items. For example, a user should be able to right-click on an assembly reference and remove it, or modify the properties of an assembly reference.

> Aside: There are exceptions to this. In practice analyzers are not directly referenced but rather brought in as part of a NuGet package. We still represent all analyzers as `IProjectTree` items directly under the "Analyzers" node. This makes it much easier for the C#/VB language service to add nodes for each diagnostic underneath the analyzer's node, and it makes it easy for the user to find so they can check the severity of the diagnostic and potentially change it using the context menu.

### DependenciesProjectTreeProvider

The primary connection point between CPS and the Dependencies node is the [`DependenciesProjectTreeProvider`][DependenciesProjectTreeProvider], implementing the `IProjectTreeProvider` interface. It is directly responsible for the following:

1. Creating the `IProjectTree` for the "Dependencies" node itself (children are handled elsewhere).
2. Handling explicit commands to copy or remove a node underneath the "Dependencies" node.
3. Mapping back and forth between `IProjectTree` instances and paths.
4. Listening for changes to the set of dependencies via the [`DependenciesSnapshotProvider`][DependenciesSnapshotProvider], delegating to the [`IDependenciesTreeViewProvider`][IDependenciesTreeViewProvider] to rebuild the tree underneath the Dependencies node, and submitting the updated tree back to CPS.

### Generating Dependencies

As evaluations and design-time builds occur, CPS pushes project changes through TPL Dataflow blocks (available via `IProjectSubscriptionService`, where `ProjectRuleSource` provides evaluation data, and `JointRuleSource` provides design-time build data).

Various implementations of [`IDependenciesRuleHandler`][IDependenciesRuleHandler] exist, and each specifies the set of rules they wish to handle (e.g. `PackageReference`, `ResolvedProjectReference`, etc.). The abstract class [`DependenciesRuleHandlerBase`][DependenciesRuleHandlerBase] exists to make implementing [`IDependenciesRuleHandler`][IDependenciesRuleHandler] easier.

The [`DependencyRulesSubscriber`][DependencyRulesSubscriber] (implementing [`IDependencyCrossTargetSubscriber`][IDependencyCrossTargetSubscriber]) subscribes via Dataflow to the union of rules specified by the handlers. When updates are received, a [`CrossTargetDependenciesChangesBuilder`][CrossTargetDependenciesChangesBuilder] is instantiated and each handler is given a chance to add/update/remove [`IDependencyModel`][IDependencyModel] instances through the builder. Once complete, the `IDependencyCrossTargetSubscriber.DependenciesChanged` event is fired, carrying dependency model changes.

Each project has an instance of [`DependenciesSnapshotProvider`][DependenciesSnapshotProvider] that holds the latest `DependenciesSnapshot` object. It imports `IDependencyCrossTargetSubscriber` implementations (such as `DependencyRulesSubscriber`) and subscribes to their `DependenciesChanged` events. When these events fire, the current snapshot is combined with changes to produce a new snapshot. That snapshot is then propagated via the `DependenciesSnapshotProvider.SnapshotChanged` event.

This `SnapshotChanged` event is then handled by:

- [`DependenciesProjectTreeProvider`][DependenciesProjectTreeProvider] to update the tree, and
- [`AggregateDependenciesSnapshotProvider`][AggregateDependenciesSnapshotProvider] which fires a solution-level `SnapshotChanged` event (useful for P2P references and graph updates, for example.)

### Translating snapshots to trees

Most of the work of translating [`IDependency`][IDependency]s to `IProjectTree`s is done by [`DependenciesTreeViewProvider`][DependenciesTreeViewProvider] (implementing [`IDependenciesTreeViewProvider`][IDependenciesTreeViewProvider]). It takes a [`DependenciesSnapshot`][DependenciesSnapshot] and generates the nodes for the target frameworks, the groupings under each framework, (Assemblies, Analyzers, Packages, Projects, etc.), and the top-level nodes under each of those groupings. In the common case that a project has a single target framework it leaves out the framework node entirely and simply hangs the different groupings directly off the `IProjectTree` for the Dependencies node.

The [`DependenciesTreeViewProvider`][DependenciesTreeViewProvider] traverses down the existing `IProjectTree` and the new [`DependenciesSnapshot`][DependenciesSnapshot] in parallel, starting from the Dependencies node itself and proceeding on to target framework, groupings, and then the individual top-level dependencies. Along the way it incrementally generates new `IProjectTree`s as it finds dependencies that have been updated, added, or removed.

> Aside: The `IProjectTree` nodes are intentionally updated from top to bottom as it prevents the Solution Explorer from collapsing expanded nodes during the update. At the very least this would be visually distracting to the user.

[`IDependency`][IDependency]s are not translated directly into `IProjectTree`s. They are first converted to [`IDependencyViewModel`][IDependencyViewModel]s and those in turn become the `IProjectTree`s. This makes it a little easier to create the `IProjectTree`s for targets and groups (e.g. the Assemblies, NuGet, Projects, etc. nodes) which are not themselves [`IDependency`][IDependency]s. In some cases a [`IDependencyModel`][IDependencyModel] may be converted directly to a [`IDependencyViewModel`][IDependencyViewModel].

### Identifiers

#### `IDependencyModel` Identifiers

Instances of [`IDependencyModel`][IDependencyModel]'s produced by an [`IProjectDependenciesSubTreeProvider`][IProjectDependenciesSubTreeProvider] must have an `Id` propety that's unique to that provider and that project.

For dependencies obtained via MSBuild evaluations (Packages, Assemblies, etc...) the `Id` is just the `OriginalItemSpec`.

#### `IDependency` Identifiers

Once a dependency model is integrated into a dependencies snapshot as an [`IDependency`][IDependency], its `Id` will be constructed from the target framework, provider type and model ID. For example: `netstandard2.0/nugetdependency/newtonsoft.json`

This allows the ID to be unique within both the provider and the target framework.

## Graph/Project System Interaction

The direct dependencies of a project will often bring in a number of transitive dependencies. For example, you may have a direct dependency on a NuGet package via a `PackageReference` element in the project file. That package causes transitive dependencies on the assemblies and analyzers within it as well as other packages it depends on. A `ProjectReference` may add transitive dependencies on other projects and packages. These dependencies form a directed graph and as such we can't properly represent them via `IProjectTree`s.

Also, the set of transitive dependencies may be very large, potentially much larger than the set of direct dependencies. For memory reasons we don't want to realize the full graph "up front". 

At the same time the user can only interact with these items in a very limited way&mdash;there's no way for them to delete an individual assembly brought in by a NuGet package for example.

These requirements lead us to represent transitive dependencies as `Microsoft.VisualStudio.GraphModel.GraphNode`s.

### DependenciesGraphProvider

The primary connection point between the Graph Nodes and the Project System is the [`DependenciesGraphProvider`][DependenciesGraphProvider] class, via the `IGraphProvider` interface. It is directly responsible for the following:

1. Specifying which graph operations it supports, via the `GetCommands` property. The only supported standard command is `GraphCommandDefinition.Contains` which is used to find the children of a given graph node.
2. Delegate the actual implementation of graph operations to various [`IDependenciesGraphActionHandler`][IDependenciesGraphActionHandler]s via the `BeginGetGraphData` method.
3. Handle the low-level mechanics of adding and removing nodes from the graph via its [`IDependenciesGraphBuilder`][IDependenciesGraphBuilder] implementation.

### Generating new `GraphNode`s

New `GraphNode`s are added to the graph as a result of operations initiated by the user.

For example, when the user expands a NuGet package node:

1. The `IGraphProvider.BeginGetGraphData` implementation in [`DependenciesGraphProvider`][DependenciesGraphProvider] is called with an `IGraphContext` describing the current graph, the input node (e.g. the node for the NuGet Package) and the operation (e.g. "get the children of the input node").
2. Each implementation of [`IDependenciesGraphActionHandler`][IDependenciesGraphActionHandler] that can handle the request is asked to do so.
3. We retrieve the current [`DependenciesSnapshot`][DependenciesSnapshot] for the project as well as the [`IDependency`][IDependency] for the input node (via [`IAggregateDependenciesSnapshotProvider`][IAggregateDependenciesSnapshotProvider]/[`DependenciesSnapshotProvider`][DependenciesSnapshotProvider]).
4. We find the first [`IDependenciesGraphViewProvider`][IDependenciesGraphViewProvider] that supports the given [`IDependency`][IDependency] and ask it to build up the corresponding parts of the graph.
5. The [`IDependenciesGraphViewProvider`][IDependenciesGraphViewProvider] decides what nodes to add to the graph, and calls [`IDependenciesGraphBuilder`][IDependenciesGraphBuilder]`.AddGraphNode` (implemented by [`DependenciesGraphProvider`][DependenciesGraphProvider]) to handle the actual mechanics.

### Connecting `GraphNode`s to `IProjectTree` nodes

CPS and the core graph logic automatically create `GraphNode`s for `IProjectTree` nodes marked with the `ProjectTreeFlags.Common.ResolvedReference` flag. For example, we will create an `IProjectTree` for a top-level NuGet package and an associated `GraphNode` will be generated automatically. We will later see this `GraphNode` as an input node in an `IGraphContext` and create and link new `GraphNode`s for its transitive dependencies.

While we don't need to create these top-level `GraphNode`s we do sometimes need to adjust their properties.

### Tracking changes to the graph

Changes to the dependencies may require that we update the graph nodes already produced. The [`DependenciesGraphChangeTracker`][DependenciesGraphChangeTracker] holds weak references to all the graphs that may need to be updated due to dependency changes, and subscribes to the [`IAggregateDependenciesSnapshotProvider`][IAggregateDependenciesSnapshotProvider]`.SnapshotChanged` event.

Whenever this event fires we find the corresponding [`IDependency`][IDependency], find the [`IDependenciesGraphViewProvider`][IDependenciesGraphViewProvider] that supports it, and delegate to the provider's `ApplyChanges` method. This generates lists of graph nodes to add and remove based on the current graph contents and the current project snapshot contents. The [`IDependenciesGraphBuilder`][IDependenciesGraphBuilder] is then called to handle the actual additions and removals.

### Identifiers

Each `GraphNode` must have a unique ID of type `Microsoft.VisualStudio.GraphModel.GraphNodeId`.

`GraphNodeId` is a recursive type, meaning an instance may be comprised of several partial `GraphNodeId` objects. This composition is performed by `GraphNodeId.GetNested`.

As mentioned earlier, `GraphNode`s are automatically created for `IProjectTree` nodes marked with the `ProjectTreeFlags.Common.ResolvedReference` flag. These graph nodes are created with IDs comprising two sub-IDs:

1. `Assembly` which identifies the project (a `Uri` of full path to the containing project file)
2. `File` which identifies the graph node within that project (a `Uri` modelling something unique about that node)
   - For top level dependency nodes, this is the numeric ID assigned to the node as a string prefixed with `>` (e.g. `>56`)
   - For child nodes (created lazily when the user expands top level nodes), this is the composite [`IDependency.Id`][IDependency] discussed above (e.g. `netstandard2.0/nugetdependency/newtonsoft.json`)

If a project is renamed, the `Assembly` URI of graph nodes within that project are automatically updated to reflect the new project path.

## Extensibility Model

Project flavors can extend the Dependencies node with additional sub-trees. To do so:

- Implement and export an [`IProjectDependenciesSubTreeProvider`][IProjectDependenciesSubTreeProvider] implementation per sub-tree
- Provide a custom implementation of [`IDependencyModel`][IDependencyModel]
- Potentially implement `IProjectTreeProvider` (usually by deriving from `ProjectTreeProviderBase`)

The _Web Tools Extensions_ project is a good example of a project flavor that does this.


[AggregateCrossTargetProjectContext]:     /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/CrossTarget/AggregateCrossTargetProjectContext.cs "AggregateCrossTargetProjectContext.cs"
[IDependenciesRuleHandler]:               /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/CrossTarget/IDependenciesRuleHandler.cs "IDependenciesRuleHandler.cs"
[DependenciesProjectTreeProvider]:        /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/DependenciesProjectTreeProvider.cs "DependenciesProjectTreeProvider.cs"
[DependenciesTreeViewProvider]:           /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/DependenciesTreeViewProvider.cs "DependenciesTreeViewProvider.cs"
[IDependenciesGraphActionHandler]:        /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/Actions/IDependenciesGraphActionHandler.cs "IDependenciesGraphActionHandler.cs"
[TrackChangesGraphActionHandler]:         /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/Actions/TrackChangesGraphActionHandler.cs "TrackChangesGraphActionHandler.cs"
[DependenciesGraphChangeTracker]:         /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/DependenciesGraphChangeTracker.cs "DependenciesGraphChangeTracker.cs"
[DependenciesGraphProvider]:              /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/DependenciesGraphProvider.cs "DependenciesGraphProvider.cs"
[IDependenciesGraphBuilder]:              /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/IDependenciesGraphBuilder.cs "IDependenciesGraphBuilder.cs"
[IDependenciesGraphViewProvider]:         /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/ViewProviders/IDependenciesGraphViewProvider.cs "IDependenciesGraphViewProvider.cs"
[ProjectGraphViewProvider]:               /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/ViewProviders/ProjectGraphViewProvider.cs "ProjectGraphViewProvider.cs"
[IDependencyModel]:                       /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/IDependencyModel.cs "IDependencyModel.cs"
[IDependenciesTreeViewProvider]:          /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/IDependenciesTreeViewProvider.cs "IDependenciesTreeViewProvider.cs"
[IProjectDependenciesSubTreeProvider]:    /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/IProjectDependenciesSubTreeProvider.cs "IProjectDependenciesSubTreeProvider.cs"
[IDependencyViewModel]:                   /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Models/IDependencyViewModel.cs "IDependencyViewModel.cs"
[AggregateDependenciesSnapshotProvider]:  /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/AggregateDependenciesSnapshotProvider.cs "AggregateDependenciesSnapshotProvider.cs"
[DependenciesSnapshot]:                   /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/DependenciesSnapshot.cs "DependenciesSnapshot.cs"
[IDependenciesSnapshotFilter]:            /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/Filters/IDependenciesSnapshotFilter.cs "IDependenciesSnapshotFilter.cs"
[IAggregateDependenciesSnapshotProvider]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/IAggregateDependenciesSnapshotProvider.cs "IAggregateDependenciesSnapshotProvider.cs"
[DependenciesSnapshot]:                   /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/DependenciesSnapshot.cs "DependenciesSnapshot.cs"
[IDependency]:                            /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/IDependency.cs "IDependency.cs"
[TargetedDependenciesSnapshot]:           /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/TargetedDependenciesSnapshot.cs "TargetedDependenciesSnapshot.cs"
[CrossTargetDependenciesChangesBuilder]:  /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/CrossTargetDependenciesChangesBuilder.cs "CrossTargetDependenciesChangesBuilder.cs"
[DependenciesRuleHandlerBase]:            /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/RuleHandlers/DependenciesRuleHandlerBase.cs "DependenciesRuleHandlerBase.cs"
[DependencyRulesSubscriber]:              /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/DependencyRulesSubscriber.cs "DependencyRulesSubscriber.cs"
[DependenciesSnapshotProvider]:           /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/DependenciesSnapshotProvider.cs "DependenciesSnapshotProvider.cs"
[IDependencyCrossTargetSubscriber]:       /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/IDependencyCrossTargetSubscriber.cs "IDependencyCrossTargetSubscriber.cs"
[ProjectRuleHandler]:                     /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/ProjectRuleHandler.cs "ProjectRuleHandler.cs"
