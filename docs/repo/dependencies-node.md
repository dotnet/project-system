# Dependencies node

This document, along with all other docs in this `docs/repo` folder, are aimed at developers of the `dotnet/project-system` repo, or extenders of the project system for other project types.

- General documentation on the dependencies tree features and diagnosing common problems is available in [Dependencies tree](../dependencies-tree.md).
- A high-level overview of the implementation of the dependencies tree is available in [Dependencies Node Roadmap](dependencies-node-roadmap.md).

---

## Customizing nodes

It is possible to modify the caption, icon and `ProjectTreeFlags` of nodes in the dependencies tree.

To do so, export an instance of `IProjectTreePropertiesProvider` as shown below. This example will rename the "Projects" node to "Applications":

```c#
[Export(ReferencesProjectTreeCustomizablePropertyValues.ContractName, typeof(IProjectTreePropertiesProvider))]
[AppliesTo(ProjectCapabilities.AlwaysApplicable)]
internal sealed class MyDependenciesTreePropertiesProvider : IProjectTreePropertiesProvider
{
    private static readonly ProjectTreeFlags ProjectDependencyGroup = ProjectTreeFlags.Create("ProjectDependencyGroup");

    public void CalculatePropertyValues(
        IProjectTreeCustomizablePropertyContext propertyContext,
        IProjectTreeCustomizablePropertyValues propertyValues)
    {
        if (propertyValues.Flags.Contains(ProjectDependencyGroup))
        {
            if (propertyValues is ReferencesProjectTreeCustomizablePropertyValues values)
            {
                // Change the caption (should be a localized string in production code)
                values.Caption = "Applications";

                // Change the icon
                values.Icon = values.ExpandedIcon = KnownMonikers.Application.ToProjectSystemType();
            }
        }
    }
}
```

---

## Handling removal of a dependency

To handle removal of the "Remove" command on a dependency in the tree, export an implementation of `IProjectTreeActionHandler`. This contract exists in `UnconfiguredProject` scope, so you can import unconfigure project MEF parts if needed.

For example:

```c#
[Export("DependencyTreeRemovalActionHandlers", typeof(IProjectTreeActionHandler))]
[AppliesTo(ProjectCapabilities.AlwaysApplicable)]
internal sealed class MyDependencyRemovalHandler : IProjectTreeActionHandler
{
    public bool CanRemove(IProjectTreeActionHandlerContext context, IEnumerable<IProjectTree> nodes, DeleteOptions deleteOptions = DeleteOptions.None)
    {
        // Block removal if required. This should be fast, as it will be called on the UI
        // thread as part of QueryStatus for commands.
        return true;
    }

    public Task RemoveAsync(IProjectTreeActionHandlerContext context, IEnumerable<IProjectTree> nodes, DeleteOptions deleteOptions = DeleteOptions.None)
    {
        // TODO respond to the removal here
        return Task.CompletedTask;
    }

    // None of the below members will be invoked for exports with contract DependencyTreeRemovalActionHandlers

    public bool CanCopy(IProjectTreeActionHandlerContext context, IEnumerable<IProjectTree> nodes, IProjectTree? receiver, bool deleteOriginal) => throw new NotImplementedException();
    public Task<bool> CanRenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node) => throw new NotImplementedException();
    public Task RenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value) => throw new NotImplementedException();
    public string? GetAddNewItemDirectory(IProjectTreeActionHandlerContext context, IProjectTree target) => throw new NotImplementedException();
    public bool CanIncludeItems(IProjectTreeActionHandlerContext context, IImmutableSet<IProjectTree> nodes) => throw new NotImplementedException();
    public Task IncludeItemsAsync(IProjectTreeActionHandlerContext context, IImmutableSet<IProjectTree> nodes) => throw new NotImplementedException();
    public bool CanExcludeItems(IProjectTreeActionHandlerContext context, IImmutableSet<IProjectTree> nodes) => throw new NotImplementedException();
    public Task ExcludeItemsAsync(IProjectTreeActionHandlerContext context, IImmutableSet<IProjectTree> nodes) => throw new NotImplementedException();
}
```

---

## Adding new dependency types

> ⚠️ NOTE the remainder of this document refers to APIs introduced in 17.7 which are currently `internal`, though may be made public in a future release. This documentation section exists to facilitate discussion around these proposed APIs. Until such a time, extenders must continue to export the `IProjectDependenciesSubTreeProvider` interface to add dependencies to the tree.

There are three ways to add items to the dependencies tree:

1. Export [`IMSBuildDependencyFactory`](#exporting-imsbuilddependencyfactory) &mdash; for configured dependencies modelled with unresolved and resolved MSBuild items.
1. Export [`IDependencySliceSubscriber`](#exporting-idependencyslicesubscriber) &mdash; for configured dependencies sourced via other means.
1. Export [`IDependencySubscriber`](#exporting-idependencysubscriber) &mdash; for unconfigured dependencies.

The distinction between configured and unconfigured dependencies is only visible when a project multi-targets, in which case any configured dependencies appear under their own target nodes (e.g. `net6.0`, `net7.0`, ...). The majority of dependencies displayed are sourced from MSBuild items and are therefore configured. An example of an unconfigured dependency type would be NPM packages, for which the concept of MSBuild configurations does not apply.

### Exporting `IMSBuildDependencyFactory`

The majority of project dependencies are exposed to the tree via MSBuild items, having two kinds of item per dependency. The unresolved item type comes from project evaluation and contains minimal information about the dependency with low latency. The resolved item type comes from design-time builds and indicates that the dependency was successfully resolved and contains richer information about the dependency, though with greater latency.

For example, `PackageReference` items pair with `ResolvedPackageReference` items, and `ProjectReference` items pair with `ResolvedProjectReference` items.

It's probably more convenient for you to derive your export from `MSBuildDependencyFactoryBase`, however you're free to implement `IMSBuildDependencyFactory` directly if you prefer.

Be sure to read the API documentation on both `IMSBuildDependencyFactory` and `MSBuildDependencyFactoryBase`.

```c#
[Export(typeof(IMSBuildDependencyFactory))]
[AppliesTo(MyProjectCapabilities.MyDependency)]
internal sealed class MyDependencyFactory : MSBuildDependencyFactoryBase
{
    // TODO review which flags apply to your nodes. DependencyFlagCache adds the default ones.
    private static readonly DependencyFlagCache s_flagCache = new(
        resolved: MyDependencyTreeFlags.MyDependency + DependencyTreeFlags.SupportsBrowse,
        unresolved: MyDependencyTreeFlags.MyDependency);

    private static DependencyGroupType s_dependencyGroupType = new(
        id: "MyDependency",
        caption: Resources.MyDependencyNodeName,
        normalGroupIcon: /* TODO specify icon */,
        warningGroupIcon: /* TODO specify icon */,
        errorGroupIcon: /* TODO specify icon */,
        groupNodeFlags: MyDependencyTreeFlags.MyDependencyGroup);

    public override DependencyGroupType DependencyGroupType => s_dependencyGroupType;

    public override string UnresolvedRuleName => MyReference.SchemaName;
    public override string ResolvedRuleName => ResolvedMyReference.SchemaName;

    public override string SchemaItemType => MyReference.PrimaryDataSourceItemType;

    public override ProjectImageMoniker Icon => /* TODO specify icon */;
    public override ProjectImageMoniker IconWarning => /* TODO specify icon */;
    public override ProjectImageMoniker IconError => /* TODO specify icon */;
    public override ProjectImageMoniker IconImplicit => /* TODO specify icon */;

    public override DependencyFlagCache FlagCache => s_flagCache;

    // TODO override any methods necessary here to modify default extraction of data from items
}
```

Some points on the above:

- You should filter your factory to a specific project capability in the `AppliesTo` expression so that your provider is not constructed for projects to which it does not apply.
- The `DependencyGroupType` object applies to the group node (e.g. `Packages`, `Projects`, ...) in the tree. The caption should be localized. You might use the same icon for the group as for specific dependencies, or use different ones.
- Include the `SupportsBrowse` capability if the resolved ItemSpec maps to the file's location on disk and you'd like the user to be able to navigate to that location via the dependency's context menu.
- You might want to override specific methods of the base class to control how data (such as caption, diagnostic level, original item spec, tree flags, icon) are created from unresolved and resolved MSBuild items.

### Exporting `IDependencySliceSubscriber`

> ⚠️ Before implementing `IDependencySliceSubscriber`, verify whether the previous approach for MSBuild items works for you. If so, it'll be simpler to use that approach instead.

If you have _configured_ dependencies that cannot be provided via `IMSBuildDependencyFactory` as described above, you can export `IDependencySliceSubscriber` yourself directly. This allows you to set up a dataflow from an instance of `IActiveConfigurationSubscriptionSource` per project configuration "slice", which will then produce data as required.

Here we see an example of chaining such data, although the dependency construction is omitted.

```c#
[Export(typeof(IDependencySliceSubscriber))]
[AppliesTo(ProjectCapability.DependenciesTree + " & " + MyProjectCapabilities.MyDependency)]
internal sealed class MyDependencySubscriber : IDependencySliceSubscriber
{
    private readonly UnconfiguredProject _unconfiguredProject;

    [ImportingConstructor]
    public MyDependencySubscriber(UnconfiguredProject unconfiguredProject)
    {
        _unconfiguredProject = unconfiguredProject;
    }

    public IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> Subscribe(ProjectConfigurationSlice slice, IActiveConfigurationSubscriptionSource source)
    {
        return new Source(_unconfiguredProject, source);
    }

    private sealed class Source : ChainedProjectValueDataSourceBase<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>
    {
        private readonly IActiveConfigurationSubscriptionSource _source;

        public Source(UnconfiguredProject unconfiguredProject, IActiveConfigurationSubscriptionSource source)
            : base(unconfiguredProject, synchronousDisposal: false, registerDataSource: false)
        {
            _source = source;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> targetBlock)
        {
            var transformBlock = DataflowBlockSlim.CreateTransformBlock<
                IProjectVersionedValue<IMySnapshot>, // TODO use whichever snapshot type your IActiveConfigurationSubscriptionSource source provides
                IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>>(
                    transformFunction: u => u.Derive(Transform),
                    nameFormat: "My Dependencies Transform {1}"); // TODO provide a proper name here

            return new DisposableBag()
            {
                // TODO replace "MySource" with whatever IActiveConfigurationSubscriptionSource data source you use
                _source.MySource.SourceBlock.LinkTo(transformBlock, DataflowOption.PropagateCompletion),

                transformBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion),

                JoinUpstreamDataSources(_source.MySource)
            };
        }

        private ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> Transform(IMySnapshot update)
        {
            // TODO use the snapshot to produce the set of `IDependency` instances you want.
            // Returning an empty `ImmutableArray<IDependency>` will create an empty group node in the tree.
            // Dependencies from multiple providers that have the same DependencyGroupType are merged under the same group node.

            return ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty;
        }
    }
}
```

- `DisposableBag` is a disposable class to which you can add multiple `IDisposable` instances and have them all disposed along with the container.
- `IActiveConfigurationSubscriptionSource` does not have a `MySource` property. You should use whichever of the data sources it does have that suit your purposes.
- Your `Transform` method should reuse previous values where possible to reduce allocations and improve performance. Many `IActiveConfigurationSubscriptionSource` data sources will provide delta updates, which you can apply to a snapshot you maintain over time, allowing reuse of as much state as possible.

### Exporting `IDependencySubscriber`

Dependencies that do not exist within any specific MSBuild configuration (e.g. `Debug|AnyCPU`, `Release|x86`, `Debug|AnyCPU|net8.0`, ...) should be provided via an export of `IDependencySubscriber`.

```c#
[Export(typeof(IDependencySubscriber))]
[AppliesTo(ProjectCapability.DependenciesTree + " & " + MyProjectCapabilities.MyDependency)]
internal sealed class MyDependencySubscriber : IDependencySubscriber
{
    private readonly IUnconfiguredProject _unconfiguredProject;
    private readonly IUnconfiguredProjectServices _unconfiguredProjectServices;

    [ImportingConstructor]
    public LegacyDependencySubscriber(
        UnconfiguredProject unconfiguredProject,
        IUnconfiguredProjectServices unconfiguredProjectServices)
    {
        _unconfiguredProject = unconfiguredProject;
        _unconfiguredProjectServices = unconfiguredProjectServices;
    }

    public IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>? Subscribe()
    {
        return new Source(_unconfiguredProject);
    }

    private sealed class Source : ProjectValueDataSourceBase<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>
    {
        private readonly IBroadcastBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> _broadcastBlock;
        private readonly IReceivableSourceBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> _publicBlock;

        private int _version;

        public Source(IUnconfiguredProjectServices unconfiguredProjectServices)
            : base(unconfiguredProjectServices, synchronousDisposal: false, registerDataSource: false)
        {
            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>>(
                nameFormat: $"My Dependency broadcast block {{1}}");

            // Publish an empty snapshot so we don't block downstream consumers.
            _broadcastBlock.Post(new ProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>(
                ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty,
                Empty.ProjectValueVersions.Add(DataSourceKey, _version)));

            _publicBlock = _broadcastBlock.SafePublicize();

            // TODO initialize code that will publish data via _broadcastBlock.Post
        }

        public override NamedIdentity DataSourceKey { get; } = new NamedIdentity(nameof(MyDependencySubscriber));

        public override IComparable DataSourceVersion => _version;

        public override IReceivableSourceBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> SourceBlock => _publicBlock;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO any disposal
            }

            base.Dispose(disposing);
        }
    }
}
```

- Exactly how and when you call `_broadcastBlock.Post` will depend upon how you obtain your dependencies.
- You should call `_broadcastBlock.Post` as soon as possible upon construction, and call again whenever dependencies are updated.
- You should increment `_version` for each publication.

---

### Modelling dependencies

Each dependency in the tree is modelled by an object that implements `IDependency`, and may also implement `IDependencyWithBrowseObject`.

- `IDependency` &mdash; base type for all dependencies in the tree.

  - `Id` &mdash; A unique identifier for the dependency within its group (and slice, for configured dependencies).
  - `Caption` &mdash; The friendly name of the dependency, for use in the UI. Although this is a display string, it rarely requires localization. It's usually just a verbatim name.
  - `Icon` &mdash; the icon to display for the dependency. Takes any diagnostic level and/or implicit state into account.
  - `Flags` &mdash; the set of `ProjectTreeFlags` applicable to the dependency.
  - `DiagnosticLevel` &mdash; the severity level of any diagnostic associated with the dependency.

- `IDependencyWithBrowseObject` &mdash; for dependencies that populate the "Properties" pane in Visual Studio, deriving from `IDependency`.

  - `UseResolvedReferenceRule` &mdash; whether the browse object for this dependency represents a resolved reference.
  - `FilePath` &mdash; the resolved path of the dependency, where appropriate, otherwise `null`.
  - `SchemaName` &mdash; the name of the rule (also known as the schema name) that backs this dependency's browse object.
  - `SchemaItemType` &mdash; the name of MSBuild item type (where relevant) that backs this dependency's browse object.
  - `BrowseObjectProperties` &mdash; the names and values of properties to use in the browse object.

Dependency group types are modelled via instances of `DependencyGroupType`. Instances of this class control the presentation (caption, icon, flags) of the parent group node in the tree, under which instances of that type's dependencies are displayed.
