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

Internally every individual dependency (both direct and transitive) is represented as an [`IDependency`][4].

All the [`IDependency`][4]s for a given target framework in a given project are collected together into an [`ITargetedDependenciesSnapshot`][5]. All of those for a given project are, in turn, collected into an [`IDependenciesSnapshot`][6].

The [`IDependenciesSnapshotProvider`][2] is responsible for providing access to the current [`IDependenciesSnapshot`][6] and firing events when the snapshot has changed. It is implemented by [`DependencySubscriptionsHost`][7].

Much of the code for the Dependencies node is concerned with creating [`IDependency`][4]s and translating them into new `IProjectTree`s when they change.

## CPS/Project System Interaction

In general, items _directly_ referenced by the project file (e.g., through `Reference`, `ProjectReference`, and `Analyzer` items in the project file) are represented with `IProjectTree` nodes as opposed to `GraphNodes`. This makes it possible for them to be represented in the project's `IVsHierarchy` and, crucially, makes it easier to code the sorts of interactions users expect for these items. For example, a user should be able to right-click on an assembly reference and remove it, or modify the properties of an assembly reference.

> Aside: There are exceptions to this. In practice analyzers are not directly referenced but rather brought in as part of a NuGet package. We still represent all analyzers as `IProjectTree` items directly under the "Analyzers" node. This makes it much easier for the C#/VB language service to add nodes for each diagnostic underneath the analyzer's node, and it makes it easy for the user to find so they can check the severity of the diagnostic and potentially change it using the context menu.

### DependenciesProjectTreeProvider

The primary connection point between CPS and the Dependencies node is the [`DependenciesProjectTreeProvider`][1], implementing the `IProjectTreeProvider` interface. It is directly responsible for the following:

1. Creating the `IProjectTree` for the "Dependencies" node itself (children are handled elsewhere).
2. Handling explicit commands to copy or remove a node underneath the "Dependencies" node.
3. Mapping back and forth between `IProjectTree` instances and paths.
4. Listening for changes to the set of dependencies via the [`IDependenciesSnapshotProvider`][2], delegating to the [`IDependenciesTreeViewProvider`][3] to rebuild the tree underneath the Dependencies node, and submitting the updated tree back to CPS.

### Generating Dependencies

As evaluations and design-time builds occur, CPS pushes project changes through TPL Dataflow blocks (available via `IProjectSubscriptionService`, where `ProjectRuleSource` provides evaluation data, and `JointRuleSource` provides design-time build data).

Various implementations of [`IDependenciesRuleHandler`][18] exist, and each specifies the set of rules they wish to handle (e.g. `PackageReference`, `ResolvedProjectReference`, etc.). The abstract class [`DependenciesRuleHandlerBase`][19] exists to make implementing [`IDependenciesRuleHandler`][18] easier.

The [`DependencyRulesSubscriber`][16] (implementing [`IDependencyCrossTargetSubscriber`][11]) subscribes via Dataflow to the union of rules specified by the handlers. When updates are received, a [`CrossTargetDependenciesChangesBuilder`][20] is instantiated and each handler is given a chance to add/update/remove `IDependencyModel` instances through the builder. Once complete, the `IDependencyCrossTargetSubscriber.DependenciesChanged` event is fired, carrying dependency model changes.

Each project has an instance of [`DependencySubscriptionsHost`][7] (implementing [`IDependenciesSnapshotProvider`][2]) that holds the latest `DependenciesSnapshot` object. It imports `IDependencyCrossTargetSubscriber` implementations (such as `DependencyRulesSubscriber`) and subscribes to their `DependenciesChanged` events. When these events fire, the current snapshot is combined with changes to produce a new snapshot. That snapshot is then propagated via the `IDependenciesSnapshotProvider.SnapshotChanged` event.

This `SnapshotChanged` event is then handled by:

- [`DependenciesProjectTreeProvider`][1] to update the tree, and
- [`AggregateDependenciesSnapshotProvider`][27] which fires a solution-level `SnapshotChanged` event (useful for P2P references and graph updates, for example.)

### Translating snapshots to trees

Most of the work of translating [`IDependency`][4]s to `IProjectTree`s is done by [`DependenciesTreeViewProvider`][8] (implementing [`IDependenciesTreeViewProvider`][3]). It takes an [`IDependenciesSnapshot`][6] and generates the nodes for the target frameworks, the groupings under each framework, (Assemblies, Analyzers, Packages, Projects, etc.), and the top-level nodes under each of those groupings. In the common case that a project has a single target framework it leaves out the framework node entirely and simply hangs the different groupings directly off the `IProjectTree` for the Dependencies node.

The [`DependenciesTreeViewProvider`][8] traverses down the existing `IProjectTree` and the new [`IDependenciesSnapshot`][6] in parallel, starting from the Dependencies node itself and proceeding on to target framework, groupings, and then the individual top-level dependencies. Along the way it incrementally generates new `IProjectTree`s as it finds dependencies that have been updated, added, or removed.

> Aside: The `IProjectTree` nodes are intentionally updated from top to bottom as it prevents the Solution Explorer from collapsing expanded nodes during the update. At the very least this would be visually distracting to the user.

[`IDependency`][4]s are not translated directly into `IProjectTree`s. They are first converted to [`IDependencyViewModel`][9]s and those in turn become the `IProjectTree`s. This makes it a little easier to create the `IProjectTree`s for targets and groups (e.g. the Assemblies, NuGet, Projects, etc. nodes) which are not themselves [`IDependency`][4]s.

### Identifiers

> TODO: Describe the role and implementation of the [`IDependency`][4]`.Id` property.

## Graph/Project System Interaction

The direct dependencies of a project will often bring in a number of transitive dependencies. For example, you may have a direct dependency on a NuGet package via a `PackageReference` element in the project file. That package causes transitive dependencies on the assemblies and analyzers within it as well as other packages it depends on. A `ProjectReference` may add transitive dependencies on other projects and packages. These dependencies form a directed graph and as such we can't properly represent them via `IProjectTree`s.

Also, the set of transitive dependencies may be very large, potentially much larger than the set of direct dependencies. For memory reasons we don't want to realize the full graph "up front". 

At the same time the user can only interact with these items in a very limited way&mdash;there's no way for them to delete an individual assembly brought in by a NuGet package for example.

These requirements lead us to represent transitive dependencies as `Microsoft.VisualStudio.GraphModel.GraphNode`s.

### DependenciesGraphProvider

The primary connection point between the Graph Nodes and the Project System is the [`DependenciesGraphProvider`][22] class, via the `IGraphProvider` interface. It is directly responsible for the following:

1. Specifying which graph operations it supports, via the `GetCommands` property. The only supported standard command is `GraphCommandDefinition.Contains` which is used to find the children of a given graph node.
2. Delegate the actual implementation of graph operations to various [`IDependenciesGraphActionHandler`][23]s via the `BeginGetGraphData` method.
3. Handle the low-level mechanics of adding and removing nodes from the graph via its [`IDependenciesGraphBuilder`][24] implementation.

### Generating new `GraphNode`s

New `GraphNode`s are added to the graph as a result of operations initiated by the user.

For example, when the user expands a NuGet package node:

1. The `IGraphProvider.BeginGetGraphData` implementation in [`DependenciesGraphProvider`][22] is called with an `IGraphContext` describing the current graph, the input node (e.g. the node for the NuGet Package) and the operation (e.g. "get the children of the input node").
2. Each implementation of [`IDependenciesGraphActionHandler`][23] that can handle the request is asked to do so.
3. We retrieve the current [`IDependenciesSnapshot`][6] for the project as well as the [`IDependency`][4] for the input node (via [`IAggregateDependenciesSnapshotProvider`][10]/[`IDependenciesSnapshotProvider`][2]).
4. We find the first [`IDependenciesGraphViewProvider`][25] that supports the given [`IDependency`][4] and ask it to build up the corresponding parts of the graph.
5. The [`IDependenciesGraphViewProvider`][25] decides what nodes to add to the graph, and calls [`IDependenciesGraphBuilder`][24]`.AddGraphNode` (implemented by [`DependenciesGraphProvider`][22]) to handle the actual mechanics.

### Connecting `GraphNode`s to `IProjectTree` nodes

CPS and the core graph logic automatically create `GraphNode`s for `IProjectTree` nodes marked with the `ProjectTreeFlags.Common.ResolvedReference` flag. For example, we will create an `IProjectTree` for a top-level NuGet package and an associated `GraphNode` will be generated automatically. We will later see this `GraphNode` as an input node in an `IGraphContext` and create and link new `GraphNode`s for its transitive dependencies.

While we don't need to create these top-level `GraphNode`s we do sometimes need to adjust their properties.

### Tracking changes to the graph

Changes to the dependencies may require that we update the graph nodes already produced. The [`DependenciesGraphChangeTracker`][28] holds weak references to all the graphs that may need to be updated due to dependency changes, and subscribes to the [`IAggregateDependenciesSnapshotProvider`][10]`.SnapshotChanged` event.

Whenever this event fires we find the corresponding [`IDependency`][4], find the [`IDependenciesGraphViewProvider`][25] that supports it, and delegate to the provider's `ApplyChanges` method. This generates lists of graph nodes to add and remove based on the current graph contents and the current project snapshot contents. The [`IDependenciesGraphBuilder`][24] is then called to handle the actual additions and removals.

### Identifiers

> TODO: Describe how identifiers work with graph nodes.

[1]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/DependenciesProjectTreeProvider.cs "DependenciesProjectTreeProvider.cs"

[2]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/IDependenciesSnapshotProvider.cs "IDependenciesSnapshotProvider.cs"

[3]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/IDependenciesTreeViewProvider.cs "IDependenciesTreeViewProvider.cs"

[4]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/IDependency.cs "IDependency.cs"

[5]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/ITargetedDependenciesSnapshot.cs "ITargetedDependenciesSnapshot.cs"

[6]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/IDependenciesSnapshot.cs "IDependenciesSnapshot.cs"

[7]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/DependencySubscriptionsHost.cs "DependencySubscriptionsHost.cs"

[8]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/DependenciesTreeViewProvider.cs "DependenciesTreeViewProvider.cs"

[9]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Models/IDependencyViewModel.cs "IDependencyViewModel.cs"

[10]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/IAggregateDependenciesSnapshotProvider.cs "IAggregateDependenciesSnapshotProvider.cs"

[11]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/IDependencyCrossTargetSubscriber.cs "IDependencyCrossTargetSubscriber.cs"

[12]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/Filters/IDependenciesSnapshotFilter.cs "IDependenciesSnapshotFilter.cs"

[13]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/IProjectDependenciesSubTreeProvider.cs "IProjectDependenciesSubTreeProvider.cs"

[14]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/CrossTarget/AggregateCrossTargetProjectContext.cs "AggregateCrossTargetProjectContext.cs"

[15]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/ProjectRuleHandler.cs "ProjectRuleHandler.cs"

[16]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/DependencyRulesSubscriber.cs "DependencyRulesSubscriber.cs"

[18]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/CrossTarget/IDependenciesRuleHandler.cs "IDependenciesRuleHandler.cs"

[19]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/DependenciesRuleHandlerBase.cs "DependenciesRuleHandlerBase.cs"

[20]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/CrossTargetDependenciesChangesBuilder.cs "CrossTargetDependenciesChangesBuilder.cs"

[21]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/DependenciesSnapshot.cs "DependenciesSnapshot.cs"

[22]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/DependenciesGraphProvider.cs "DependenciesGraphProvider.cs"

[23]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/Actions/IDependenciesGraphActionHandler.cs "IDependenciesGraphActionHandler.cs"

[24]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/IDependenciesGraphBuilder.cs "IDependenciesGraphBuilder.cs"

[25]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/ViewProviders/IDependenciesGraphViewProvider.cs "IDependenciesGraphViewProvider.cs"

[26]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/Actions/TrackChangesGraphActionHandler.cs "TrackChangesGraphActionHandler.cs"

[27]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/AggregateDependenciesSnapshotProvider.cs "AggregateDependenciesSnapshotProvider.cs"

[28]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/DependenciesGraphChangeTracker.cs "DependenciesGraphChangeTracker.cs"

[29]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GraphNodes/ViewProviders/ProjectGraphViewProvider.cs "ProjectGraphViewProvider.cs"
