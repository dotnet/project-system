// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Legacy;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;

using DependenciesSnapshotInput = (
    System.Collections.Immutable.ImmutableDictionary<Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.DependencyGroupType, System.Collections.Immutable.ImmutableArray<Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.IDependency>> UnconfiguredDependencies,
    System.Collections.Immutable.ImmutableDictionary<Microsoft.VisualStudio.ProjectSystem.ProjectConfigurationSlice, Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.DependenciesSnapshotSlice> ConfiguredDependencies,
    Microsoft.VisualStudio.ProjectSystem.ConfiguredProject ActiveConfiguredProject);

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

/// <summary>
/// Provides immutable snapshot of all dependencies within a project, across all types (packages, projects, ...) and slices (target frameworks, ...).
/// </summary>
/// <remarks>
/// Produced snapshots include dependencies gathered via two interfaces:
/// <list type="bullet">
///   <item><see cref="IDependencySliceSubscriber"/> for configured (per-slice) dependency types.</item>
///   <item><see cref="IDependencySubscriber"/> for unconfigured dependency types.</item>
/// </list>
/// <para>
/// This component uses <see cref="IActiveConfigurationGroupSubscriptionService"/> to subscribe to the project's "slices",
/// creating subscriptions within each slice for each <see cref="IDependencySliceSubscriber"/>.
/// </para>
/// <para>
/// Additionally, any <see cref="IDependencySubscriber"/> is asked to contribute unconfigured dependencies, that exist outside
/// of any given slice. For example, in JavaScript/TypeScript projects, NPM packages exist outside of any configuration and
/// are gathered and displayed accordingly.
/// </para>
/// <para>
/// Note the interface <see cref="VS.Tree.Dependencies.IProjectDependenciesSubTreeProvider"/> was used historically as an extension
/// point for providing dependencies, and is used in web projects for NPM packages. Support for that interface is provided internally
/// via <see cref="LegacyDependencySubscriber"/> which implements <see cref="IDependencySubscriber"/>.
/// </para>
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
[Export(typeof(DependenciesSnapshotProvider))]
[AppliesTo(ProjectCapability.DependenciesTree)]
internal sealed class DependenciesSnapshotProvider : OnceInitializedOnceDisposedAsync
{
    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly IUnconfiguredProjectTasksService _tasksService;
    private readonly IActiveConfigurationGroupSubscriptionService _activeConfigurationGroupSubscriptionService;
    private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;
    private readonly IProjectThreadingService _threadingService;
    private readonly IProjectFaultHandlerService _projectFaultHandler;
    private readonly IBroadcastBlock<IProjectVersionedValue<DependenciesSnapshot>> _source;
    private readonly IReceivableSourceBlock<IProjectVersionedValue<DependenciesSnapshot>> _publicSource;
    private readonly DisposableBag _disposables;

    [ImportMany] private readonly OrderPrecedenceImportCollection<IDependencySubscriber> _dependencySubscribers;
    [ImportMany] private readonly OrderPrecedenceImportCollection<IDependencySliceSubscriber> _dependencySliceSubscribers;

    [ImportingConstructor]
    public DependenciesSnapshotProvider(
        UnconfiguredProject unconfiguredProject,
        IUnconfiguredProjectCommonServices commonServices,
        IUnconfiguredProjectTasksService tasksService,
        IActiveConfigurationGroupSubscriptionService activeConfigurationGroupSubscriptionService,
        IActiveConfiguredProjectProvider activeConfiguredProjectProvider,
        IProjectThreadingService threadingService,
        IProjectFaultHandlerService projectFaultHandler)
        : base(commonServices.ThreadingService.JoinableTaskContext)
    {
        _unconfiguredProject = unconfiguredProject;
        _tasksService = tasksService;
        _activeConfigurationGroupSubscriptionService = activeConfigurationGroupSubscriptionService;
        _activeConfiguredProjectProvider = activeConfiguredProjectProvider;
        _threadingService = threadingService;
        _projectFaultHandler = projectFaultHandler;

        _dependencySubscribers = new OrderPrecedenceImportCollection<IDependencySubscriber>(
            projectCapabilityCheckProvider: _unconfiguredProject);

        _dependencySliceSubscribers = new OrderPrecedenceImportCollection<IDependencySliceSubscriber>(
            projectCapabilityCheckProvider: _unconfiguredProject);

        _source = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<DependenciesSnapshot>>(nameFormat: "DependenciesSnapshot broadcast: {1}", skipIntermediateInputData: true);
        _publicSource = _source.SafePublicize("DependenciesSnapshot broadcast (public): {1}");

        _projectFaultHandler.RegisterFaultHandler(
            _source,
            _unconfiguredProject,
            ProjectFaultSeverity.LimitedFunctionality);

        _disposables = new DisposableBag { new DisposableDelegate(_source.Complete) };
    }

    public IReceivableSourceBlock<IProjectVersionedValue<DependenciesSnapshot>> Source => _publicSource;

    internal Task EnsureInitializedAsync()
    {
        return InitializeAsync();
    }

    protected override Task DisposeCoreAsync(bool initialized)
    {
        _disposables.Dispose();

        return Task.CompletedTask;
    }

    protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        // Ensure the project doesn't unload during subscription.
        return _tasksService.LoadedProjectAsync(async () =>
        {
            try
            {
                await InitializeAsync();
            }
            catch (Exception exception)
            {
                _source.Fault(exception);
                throw;
            }
        });

        Task InitializeAsync()
        {
            DependenciesSnapshot? lastSnapshot = null;

            // Make a snapshot of these dynamic imports, so that we operate on consistent data.
            // Note, we don't support dynamic modification of these exports (in response to changing project capabilities).
            ImmutableArray<IDependencySubscriber> dependencySubscribers = _dependencySubscribers.ToImmutableValueArray();
            ImmutableArray<IDependencySliceSubscriber> dependencySliceSubscribers = _dependencySliceSubscribers.ToImmutableValueArray();

            // We will create two kinds of subscription, one for unconfigured and one for configured dependencies.
            // Each subscription contains a Dataflow data source block that we will combine through additional blocks to produce
            // the final dependency snapshot for the project.
            //
            // In the case of configured dependencies, there are some earlier blocks too that determine the set of configuration
            // slices for which subscriptions should be made. We have one configured subscription per slice, per IDependencySliceSubscriber instance.
            //
            // For unconfigured dependencies, we have one subscription per IDependencySubscriber instance.

            ISourceBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> unconfiguredSource = SubscribeUnconfigured();

            ISourceBlock<IProjectVersionedValue<ImmutableDictionary<ProjectConfigurationSlice, DependenciesSnapshotSlice>>> configuredSource = SubscribeConfigured();

            var transformBlock = DataflowBlockSlim.CreateTransformManyBlock<
                IProjectVersionedValue<DependenciesSnapshotInput>,
                IProjectVersionedValue<DependenciesSnapshot>>(
                    MergeFinalDependenciesSnapshot,
                    nameFormat: "Dependencies final merge {1}",
                    skipIntermediateInputData: true,
                    skipIntermediateOutputData: true,
                    cancellationToken: cancellationToken);

            _disposables.Add(
                ProjectDataSources.SyncLinkTo(
                    unconfiguredSource.SyncLinkOptions(),
                    configuredSource.SyncLinkOptions(),
                    _activeConfiguredProjectProvider.SourceBlock.SyncLinkOptions(),
                    target: transformBlock,
                    linkOptions: DataflowOption.PropagateCompletion,
                    cancellationToken: cancellationToken));

            _disposables.Add(transformBlock.LinkTo(_source, DataflowOption.PropagateCompletion));

            _disposables.Add(ProjectDataSources.JoinUpstreamDataSources(_threadingService.JoinableTaskFactory, _projectFaultHandler, _activeConfiguredProjectProvider));

            return Task.CompletedTask;

            ISourceBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> SubscribeUnconfigured()
            {
                List<IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> subscriptions = new();

                foreach (IDependencySubscriber dependencySubscriber in dependencySubscribers)
                {
                    IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>? subscription = dependencySubscriber.Subscribe();

                    if (subscription is not null)
                    {
                        subscriptions.Add(subscription);
                    }
                }

                if (subscriptions.Count == 0)
                {
                    // Optimize the common case where no unconfigured subscriptions exist.
                    var block = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>>(
                        nameFormat: "Empty unconfigured dependency broadcast {1}");
                    block.Post(new ProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>(
                        ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty,
                        Empty.ProjectValueVersions));
                    return block;
                }

                // We "unwrap" data from our collection of data sources, such that we have a collection where each data source provides a single value.
                var unwrapSlicesBlock = new UnwrapCollectionChainedProjectValueDataSource<
                    ImmutableArray<IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>>,
                    ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>(
                        _unconfiguredProject,
                        getDataSource: static sources => sources);
                _disposables.Add(unwrapSlicesBlock);

                unwrapSlicesBlock.Post(
                    new ProjectVersionedValue<ImmutableArray<IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>>>(
                        subscriptions.ToImmutableArray(),
                        Empty.ProjectValueVersions));

                var mergeBlock = DataflowBlockSlim.CreateTransformBlock<
                    IProjectVersionedValue<IReadOnlyCollection<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>>,
                    IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>>(
                        transformFunction: u => u.Derive(MergeUnconfiguredDependencies),
                        nameFormat: "Unconfigured dependencies merge {1}");

                _disposables.Add(unwrapSlicesBlock.SourceBlock.LinkTo(mergeBlock, DataflowOption.PropagateCompletion));

                _disposables.Add(
                    new DisposableDelegate(() =>
                    {
                        foreach (IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> subscription in subscriptions)
                        {
                            (subscription as IDisposable)?.Dispose();
                        }
                    }));

                return mergeBlock;

                ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> MergeUnconfiguredDependencies(IReadOnlyCollection<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> groups)
                {
                    if (groups.Count == 0)
                    {
                        // Optimize the common case where there are no unconfigured dependency group types.
                        return ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty;
                    }

                    Dictionary<DependencyGroupType, List<IDependency>> dependenciesByType = new();

                    foreach (ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> update in groups)
                    {
                        foreach ((DependencyGroupType type, ImmutableArray<IDependency> dependenciesToAdd) in update)
                        {
                            if (!dependenciesByType.TryGetValue(type, out List<IDependency>? dependencies))
                            {
                                dependenciesByType.Add(type, dependencies = new());
                            }

                            dependencies.AddRange(dependenciesToAdd);
                        }
                    }

                    return dependenciesByType.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray());
                }
            }

            ISourceBlock<IProjectVersionedValue<ImmutableDictionary<ProjectConfigurationSlice, DependenciesSnapshotSlice>>> SubscribeConfigured()
            {
                Dictionary<ProjectConfigurationSlice, SliceDataSource> dataSourceBySlice = new();

                // We transform from the set of slices to a set of per-slice data sources.
                var transformBlock = DataflowBlockSlim.CreateTransformBlock<
                    IProjectVersionedValue<ConfigurationSubscriptionSources>,
                    IProjectVersionedValue<ImmutableArray<SliceDataSource>>>(
                        transformFunction: SlicesToDataSources,
                        nameFormat: "Dependency slices-to-data-sources transform {1}");

                // Link slices into the transform block.
                _disposables.Add(_activeConfigurationGroupSubscriptionService.SourceBlock.LinkTo(transformBlock, DataflowOption.PropagateCompletion));

                // We "unwrap" data from our collection of data sources, such that we have a collection where each data source provides a single value.
                var unwrapSlicesBlock = new UnwrapCollectionChainedProjectValueDataSource<
                    ImmutableArray<SliceDataSource>,
                    DependenciesSnapshotSlice>(
                        _unconfiguredProject,
                        getDataSource: static sources => sources);
                _disposables.Add(unwrapSlicesBlock);

                // Link transform to unwrap.
                _disposables.Add(transformBlock.LinkTo(unwrapSlicesBlock, DataflowOption.PropagateCompletion));

                // Merge dependencies across all slices.
                var mergeBlock = DataflowBlockSlim.CreateTransformBlock<
                    IProjectVersionedValue<IReadOnlyCollection<DependenciesSnapshotSlice>>,
                    IProjectVersionedValue<ImmutableDictionary<ProjectConfigurationSlice, DependenciesSnapshotSlice>>>(
                        static data => data.Derive(
                            static updates => updates.ToImmutableDictionary(
                                update => update.Slice,
                                update => update)),
                        nameFormat: "Merge dependencies across slices {1}",
                        skipIntermediateInputData: false);

                _disposables.Add(unwrapSlicesBlock.SourceBlock.LinkTo(mergeBlock, DataflowOption.PropagateCompletion));

                _disposables.Add(ProjectDataSources.JoinUpstreamDataSources(_threadingService.JoinableTaskFactory, _projectFaultHandler, _activeConfigurationGroupSubscriptionService));

                _disposables.Add(
                    new DisposableDelegate(() =>
                    {
                        foreach ((_, SliceDataSource dataSource) in dataSourceBySlice)
                        {
                            dataSource.Dispose();
                        }
                    }));

                return mergeBlock;

                IProjectVersionedValue<ImmutableArray<SliceDataSource>> SlicesToDataSources(IProjectVersionedValue<ConfigurationSubscriptionSources> update)
                {
                    ConfigurationSubscriptionSources sources = update.Value;

                    // Check off existing slices. Any unseen at the end must be disposed.
                    var checklist = new Dictionary<ProjectConfigurationSlice, SliceDataSource>(dataSourceBySlice);

                    foreach ((ProjectConfigurationSlice slice, IActiveConfigurationSubscriptionSource source) in sources)
                    {
                        if (!dataSourceBySlice.TryGetValue(slice, out SliceDataSource dataSource))
                        {
                            // New slice.
                            Assumes.False(checklist.ContainsKey(slice));

                            dataSource = new SliceDataSource(_unconfiguredProject, slice, source, dependencySliceSubscribers);

                            dataSourceBySlice.Add(slice, dataSource);
                        }
                        else
                        {
                            // We have seen this slice, so remove it from the list we're tracking.
                            Assumes.True(checklist.Remove(slice));
                        }
                    }

                    // Dispose data sources for unseen slices.
                    foreach ((ProjectConfigurationSlice slice, SliceDataSource dataSource) in checklist)
                    {
                        Assumes.True(dataSourceBySlice.Remove(slice));

                        dataSource.Dispose();
                    }

                    // TODO how often do we get here without changing anything? would it help to cache the prior result?
                    return update.Derive(u => dataSourceBySlice.Values.ToImmutableArray());
                }
            }

            IEnumerable<IProjectVersionedValue<DependenciesSnapshot>> MergeFinalDependenciesSnapshot(IProjectVersionedValue<DependenciesSnapshotInput> input)
            {
                DependenciesSnapshotInput update = input.Value;

                ProjectConfiguration activeProjectConfiguration = update.ActiveConfiguredProject.ProjectConfiguration;

                ProjectConfigurationSlice? primarySlice = update.ConfiguredDependencies.Keys.FirstOrDefault(static (slice, config) => slice.IsPrimaryActiveSlice(config), activeProjectConfiguration);

                if (primarySlice is null)
                {
                    // We cannot tell which slice is primary, so don't return any snapshot just yet. A future update will restore this state.
                    // This is rare, but NFE data shows it can happen. It's not possible to sync link these streams reliably, as we remove
                    // all configured-project versions from per-slice data.
                    yield break;
                }

                if (lastSnapshot is null)
                {
                    lastSnapshot = new DependenciesSnapshot(primarySlice, update.ConfiguredDependencies, update.UnconfiguredDependencies);
                }
                else
                {
                    lastSnapshot = lastSnapshot.Update(primarySlice, update.ConfiguredDependencies, update.UnconfiguredDependencies);
                }

                yield return new ProjectVersionedValue<DependenciesSnapshot>(lastSnapshot, input.DataSourceVersions);
            }
        }
    }

    /// <summary>
    /// An <see cref="IProjectValueDataSource"/> for all dependency and project data from a given slice.
    /// </summary>
    private sealed class SliceDataSource : ChainedProjectValueDataSourceBase<DependenciesSnapshotSlice>
    {
        private readonly ProjectConfigurationSlice _slice;
        private readonly IActiveConfigurationSubscriptionSource _source;
        private readonly ImmutableArray<IDependencySliceSubscriber> _dependencySliceSubscribers;

        public SliceDataSource(UnconfiguredProject unconfiguredProject, ProjectConfigurationSlice slice, IActiveConfigurationSubscriptionSource source, ImmutableArray<IDependencySliceSubscriber> dependencySliceSubscribers)
            : base(unconfiguredProject, synchronousDisposal: false, registerDataSource: false)
        {
            _slice = slice;
            _source = source;
            _dependencySliceSubscribers = dependencySliceSubscribers;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<DependenciesSnapshotSlice>> targetBlock)
        {
            // We have two streams of configured data.
            //
            // 1. Data about the slices themselves:
            //    - The active ConfiguredProject within the slice.
            //    - The IProjectCatalogSnapshot for the configuration, for use when populating browse objects.
            // 2. The actual dependencies within the slices, sourced from IDependencySliceSubscriber instances.

            DependenciesSnapshotSlice? snapshot = null;

            var disposables = new DisposableBag();

            var sliceSources = ImmutableList.CreateBuilder<ProjectDataSources.SourceBlockAndLink<IProjectValueVersions>>();

            sliceSources.Add(ConfiguredDependencyFilterBlock.TransformSource(_source.ActiveConfiguredProjectSource.SourceBlock, disposables, "Transformed ActiveConfiguredProjectSource {1}").SyncLinkOptions<IProjectValueVersions>());
            sliceSources.Add(ConfiguredDependencyFilterBlock.TransformSource(_source.ProjectCatalogSource.SourceBlock, disposables, "Transformed ProjectCatalogSource {1}").SyncLinkOptions<IProjectValueVersions>());

            disposables.Add(JoinUpstreamDataSources(_source.ActiveConfiguredProjectSource, _source.ProjectCatalogSource));

            foreach (IDependencySliceSubscriber subscriber in _dependencySliceSubscribers)
            {
                IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> subscriptionSource = subscriber.Subscribe(_slice, _source);

                disposables.Add(JoinUpstreamDataSources(subscriptionSource));

                if (subscriptionSource is IDisposable disposable)
                {
                    disposables.Add(disposable);
                }

                sliceSources.Add(ConfiguredDependencyFilterBlock.TransformSource(subscriptionSource.SourceBlock, disposables, "Transformed IDependencySliceSubscriber {1}").SyncLinkOptions<IProjectValueVersions>());
            }

            var mergeBlock = DataflowBlockSlim.CreateTransformBlock<
                Tuple<ImmutableList<IProjectValueVersions>, IImmutableDictionary<NamedIdentity, IComparable>>,
                IProjectVersionedValue<DependenciesSnapshotSlice>>(MergeDataIntoSliceSnapshot);

            disposables.Add(ProjectDataSources.SyncLinkTo(
                sliceSources.ToImmutable(),
                mergeBlock,
                DataflowOption.PropagateCompletion));

            disposables.Add(mergeBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion));

            return disposables;

            IProjectVersionedValue<DependenciesSnapshotSlice> MergeDataIntoSliceSnapshot(Tuple<ImmutableList<IProjectValueVersions>, IImmutableDictionary<NamedIdentity, IComparable>> tuple)
            {
                ImmutableList<IProjectValueVersions> versionedValues = tuple.Item1;
                IImmutableDictionary<NamedIdentity, IComparable> versions = tuple.Item2;

                Assumes.True(versionedValues.Count >= 2);
                Assumes.False(versions.ContainsKey(ProjectDataSources.ConfiguredProjectIdentity), $"Update should not contain {nameof(ProjectDataSources.ConfiguredProjectIdentity)}.");
                Assumes.False(versions.ContainsKey(ProjectDataSources.ConfiguredProjectVersion), $"Update should not contain {nameof(ProjectDataSources.ConfiguredProjectVersion)}.");

                ImmutableList<IProjectValueVersions>.Enumerator enumerator = versionedValues.GetEnumerator();

                Assumes.True(enumerator.MoveNext());

                ConfiguredProject configuredProject = ((IProjectVersionedValue<ConfiguredProject>)enumerator.Current).Value;

                Assumes.True(enumerator.MoveNext());

                IProjectCatalogSnapshot catalogs = ((IProjectVersionedValue<IProjectCatalogSnapshot>)enumerator.Current).Value;

                List<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> dependencySliceUpdates = new(versionedValues.Count - 2);

                while (enumerator.MoveNext())
                {
                    dependencySliceUpdates.Add(((IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>)enumerator.Current).Value);
                }

                DependenciesSnapshotSlice.Update(ref snapshot, _slice, configuredProject, catalogs, dependencySliceUpdates);

                return new ProjectVersionedValue<DependenciesSnapshotSlice>(snapshot, versions);
            }
        }
    }
}
