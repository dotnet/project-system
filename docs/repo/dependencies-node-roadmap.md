# Dependencies Node Roadmap

This document describes, at a high level, the design and implementation of the Dependencies node with pointers to the important types.

Let's start at the top and work our way down.

## The CPS View of the Project Tree

On the CPS side the project tree is composed of instances of the `IProjectTree` and `IProjectItemTree` interfaces. `IProjectTree` captures the structure of the tree (e.g. it has properties to access the parent and child nodes) and the "UI" aspects of the node--name, icons, visibility, etc. An `IProjectItemTree` captures all of that but also represents a concrete item within the project like a file, assembly reference, or NuGet package.

An `IProjectTree` is immutable. When a part of the tree needs to be updated, we need to replace it and form a new tree.

Components wishing to add items to the project tree we must implement and export the `IProjectTreeProvider` interface. In practice we implement the interface by deriving from the abstract `ProjectTreeProviderBase` class provided by CPS. In general, we receive events/messages from CPS detailing changes to the project, generated an updated `IProjectTree`, and then pass it back to CPS via `ProjectTreeProviderBase.SubmitTreeUpdateAsync`.

## The Project System View of the Project Tree

Internally every individual dependency (both direct and transitive) is represented as an [`IDependency`][4].

All the [`IDependency`][4]s for a given target framework in a given project are collected together into an [`ITargetedDependenciesSnapshot`][5]. All of those for a given project are, in turn, collected into an [`IDependenciesSnapshot`][6].

The [`IDependenciesSnapshotProvider`][2] is responsible for providing access to the current [`IDependenciesSnapshot`][6] and firing events when the snapshot has changed. It is implemented by [`DependencySubscriptionsHost`][7].

Much of the code for the Dependencies node is concerned with creating [`IDependency`][4]s and translating them into new `IProjectTree`s when they change.

## DependenciesProjectTreeProvider

The primary connection point between CPS and the Dependencies node is the [`DependenciesProjectTreeProvider`][1], implementing the the `IProjectTreeProvider` interface. It is directly responsible for the following:

1. Creating the `IProjectTree` for the "Dependencies" node itself (children are handled elsewhere).
2. Handling explicit commands to copy or remove a node underneath the "Dependencies" node.
3. Mapping back and forth between `IProjectTree` instances and paths.
4. Listening for changes to the set of dependencies via the [`IDependenciesSnapshotProvider`][2], delegating to the [`IDependenciesTreeViewProvider`][3] to rebuild the tree underneath the Dependencies node, and submitting the updated tree back to CPS.

## Generating Dependencies

As the project changes due to evaluations and design-time builds CPS pushes changes through TPL dataflow blocks. The [`DependencyRulesSubscriber`][16] (implementing [`IDependencyCrossTargetSubscriber`][11] via [`CrossTargetRuleSubscriberBase<T>`][17]) receives this data. The various implementations of [`ICrossTargetRuleHandler<T>`][18] both specify what rules to listen for and handle processing changes to the associated items--see the `HandleAsync` methods in both [`CrossTargetRuleSubscriberBase<T>`][17] and [`DependenciesRuleHandlerBase`][19].

These handlers add the added and removed items to a [`DependenciesRuleChangeContext`][20]. This fires its `DependenciesChanged` event and passes along the [`DependenciesRuleChangeContext`][20] to event subscribers.

Each project has an instance of [`DependencySubscriptionsHost`][7] implementing [`IDependenciesSnapshotProvider`][2]. It subscribes to this `DependenciesChanged` event, ultimately handling it in `UpdateDependenciesSnapshotAsync`. If there are relevant changes it creates a new [`DependenciesSnapshot`][21] and fires its own `SnapshotChanged` event.

The [`DependenciesProjectTreeProvider`][1] subscribes to this event and handles updating the tree.

## Translating snapshots to trees

Most of the work of translating [`IDependency`][4]s to `IProjectTree`s is done by [`IDependenciesTreeViewProvider`][3] and its singular implementation, [`GroupedByTargetTreeViewProvider`][8]. It takes an [`IDependenciesSnapshot`][6] and generates the nodes for the target frameworks, the groupings under each framework, (Assemblies, Analyzers, Packages, Projects, etc.), and the top-level nodes under each of those groupings. In the common case that a project has a single target framework it leaves out the framework node entirely and simply hangs the different groupings directly off the `IProjectTree` for the Dependencies node.

> Aside: While there is currently only one implementation of [`IDependenciesTreeViewProvider`][3] you could imagine others that organize the nodes in different ways: flattening the top-level items into a single list or organizing nodes first by type then by target framework. Should we create other implementations we would also need some way to switch between them; there is currently no support for doing so.

The [`GroupedByTargetTreeViewProvider`][8] traverses down the existing `IProjectTree` and the new [`IDependenciesSnapshot`][6] in parallel starting from the Dependencies node itself and proceeding on to target framework, groupings, and then the individual top-level dependencies. Along the way it incrementally generates new `IProjectTree`s as it finds dependencies that have been updated, added, or removed.

[`IDependency`][4]s are not translated directly into `IProjectTree`s. They are first converted to [`IDependencyViewModel`][9]s and those in turn become the `IProjectTree`s. This makes it a little easier to create the `IProjectTree`s for targets and groups (e.g. the Assemblies, NuGet, Projects, etc. nodes) which are not themselves [`IDependency`][4]s.

## Identifiers

TODO: Describe the role and implementation of the [`IDependency`][4]`.Id` property.

## Graph Nodes

Much of this document only applies to the assemblies, packages, analyzers, etc. that are _directly_ part of the project. These make up the first level of items under the Assemblies, NuGet, Analyzers, Projects, etc. nodes under Dependencies. The _transitive_ dependencies shown under that first level are not implemented as `IProjectTree` instances, but rather as graph nodes.

TODO: Describe the motivation behind and implementation of graph nodes for representing transitive dependencies.

[1]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/DependenciesProjectTreeProvider.cs "DependenciesProjectTreeProvider.cs"

[2]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/IDependenciesSnapshotProvider.cs "IDependenciesSnapshotProvider.cs"

[3]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/IDependenciesTreeViewProvider.cs "IDependenciesTreeViewProvider.cs"

[4]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/IDependency.cs "IDependency.cs"

[5]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/ITargetedDependenciesSnapshot.cs "ITargetedDependenciesSnapshot.cs"

[6]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/IDependenciesSnapshot.cs "IDependenciesSnapshot.cs"

[7]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/DependencySubscriptionsHost.cs "DependencySubscriptionsHost.cs"

[8]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/GroupedByTargetTreeViewProvider.cs "GroupedByTargetTreeViewProvider.cs"

[9]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Models/IDependencyViewModel.cs "IDependencyViewModel.cs"

[10]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/IAggregateDependenciesSnapshotProvider.cs "IAggregateDependenciesSnapshotProvider.cs"

[11]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/IDependencyCrossTargetSubscriber.cs "IDependencyCrossTargetSubscriber.cs"

[12]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/Filters/IDependenciesSnapshotFilter.cs "IDependenciesSnapshotFilter.cs"

[13]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/IProjectDependenciesSubTreeProvider.cs "IProjectDependenciesSubTreeProvider.cs"

[14]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/CrossTarget/AggregateCrossTargetProjectContext.cs "AggregateCrossTargetProjectContext.cs"

[15]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/ProjectRuleHandler.cs "ProjectRuleHandler.cs"

[16]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/DependencyRulesSubscriber.cs "DependencyRulesSubscriber.cs"

[17]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/CrossTarget/CrossTargetRuleSubscriberBase.cs "CrossTargetRuleSubscriberBase.cs"

[18]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/CrossTarget/ICrossTargetRuleHandler.cs "ICrossTargetRuleHandler.cs"

[19]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/DependenciesRuleHandlerBase.cs "DependenciesRuleHandlerBase.cs"

[20]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Subscriptions/DependenciesRuleChangeContext.cs "DependenciesRuleChangeContext.cs"

[21]: /src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Tree/Dependencies/Snapshot/DependenciesSnapshot.cs "DependenciesSnapshot.cs"