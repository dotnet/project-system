// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Legacy;

/// <summary>
/// Bridges between the legacy <see cref="IProjectDependenciesSubTreeProvider"/> interface, which uses C# events
/// to notify of updates with deltas, and the newer <see cref="IDependencySubscriber"/> which uses Dataflow and
/// snapshots.
/// </summary>
/// <remarks>
/// <para>
/// This preserves support for legacy dependencies, such as NPM packages in WebTools projects, until they are
/// ready to migrate to the newer APIs.
/// </para>
/// <para>
/// The legacy API (<see cref="IProjectDependenciesSubTreeProvider"/>) supports reporting dependencies
/// per-target-framework, however no known implementations do this. In this newer version of the dependencies
/// tree code, we treat all dependencies provided via this interface as unconfigured, and ignore the target
/// framework value specified via <see cref="DependenciesChangedEventArgs"/>.
/// </para>
/// </remarks>
[Export(typeof(IDependencySubscriber))]
[AppliesTo(ProjectCapability.DependenciesTree)]
internal sealed class LegacyDependencySubscriber : IDependencySubscriber
{
    [ImportMany] private readonly OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider> _subTreeProviders;

    private readonly IUnconfiguredProjectServices _unconfiguredProjectServices;

    [ImportingConstructor]
    public LegacyDependencySubscriber(
        UnconfiguredProject unconfiguredProject,
        IUnconfiguredProjectServices unconfiguredProjectServices)
    {
        _unconfiguredProjectServices = unconfiguredProjectServices;

        _subTreeProviders = new OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider>(
            ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
            projectCapabilityCheckProvider: unconfiguredProject);
    }

    public IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>? Subscribe()
    {
        // Make a snapshot of these dynamic imports, so that we operate on consistent data.
        // Note, we don't support dynamic modification of these exports (in response to changing project capabilities).
        ImmutableArray<IProjectDependenciesSubTreeProvider> subTreeProviders = _subTreeProviders.ToImmutableValueArray();

        if (subTreeProviders.Length == 0)
        {
            // There are no legacy providers, so we don't need to do anything extra here.
            return null;
        }

        return new Source(_unconfiguredProjectServices, subTreeProviders);
    }

    private sealed class ProviderState
    {
        private readonly DependencyGroupType _dependencyType;
        private readonly Dictionary<string, IDependency> _dependencyById = new(StringComparers.DependencyIds);

        private ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> _snapshot;

        public ProviderState(IProjectDependenciesSubTreeProvider provider)
        {
            IDependencyModel rootNode = provider.CreateRootDependencyNode();

            _dependencyType = new(
                id: rootNode.ProviderType,
                caption: rootNode.Caption,
                normalGroupIcon: rootNode.Icon.ToProjectSystemType(),
                warningGroupIcon: rootNode.UnresolvedIcon.ToProjectSystemType(),
                errorGroupIcon: rootNode.UnresolvedIcon.ToProjectSystemType(),
                groupNodeFlags: rootNode.Flags);

            _snapshot = ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty
                .Add(_dependencyType, ImmutableArray<IDependency>.Empty);
        }

        public ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> Update(IDependenciesChanges changes)
        {
            bool anyChanges = false;

            if (changes.RemovedNodes.Count != 0)
            {
                foreach (IDependencyModel removed in changes.RemovedNodes)
                {
                    _dependencyById.Remove(removed.Id);
                }

                anyChanges = true;
            }

            if (changes.AddedNodes.Count != 0)
            {
                foreach (IDependencyModel added in changes.AddedNodes)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    // NOTE we still need to check this in case extensions (eg. WebTools) provide us with top level items that need to be filtered out
                    if (!added.TopLevel)
                    {
                        continue;
                    }
#pragma warning restore CS0618 // Type or member is obsolete

                    IDependency dependency = ToDependency(added);

                    _dependencyById[dependency.Id] = dependency;

                    anyChanges = true;
                }
            }

            if (anyChanges)
            {
                if (_dependencyById.Count == 0)
                {
                    // No dependencies exist, so remove it from the snapshot to prevent us displaying an
                    // empty group node with no children.
                    _snapshot = _snapshot.Remove(_dependencyType);
                }
                else
                {
                    _snapshot = _snapshot.SetItem(_dependencyType, _dependencyById.Values.ToImmutableArray());
                }
            }

            return _snapshot;

            static IDependency ToDependency(IDependencyModel model)
            {
                // NOTE we don't use the expanded icon for dependencies, and legacy dependencies don't support "implicit" icons either.
                ImageMoniker icon = model.Resolved ? model.Icon : model.UnresolvedIcon;

                ProjectTreeFlags flags = model.Flags;

                // Just in case legacy providers don't pass correct flags, update them here.
                if (model.Resolved)
                {
                    if (!flags.Contains(ProjectTreeFlags.ResolvedReference))
                    {
                        flags += ProjectTreeFlags.ResolvedReference;
                    }
                }
                else
                {
                    if (!flags.Contains(ProjectTreeFlags.BrokenReference))
                    {
                        flags += ProjectTreeFlags.BrokenReference;
                    }
                }

                return new Dependency(
                    id: model.Id,
                    caption: model.Caption ?? "",
                    icon: icon.ToProjectSystemType(),
                    flags: flags,
                    diagnosticLevel: model.Resolved ? DiagnosticLevel.None : DiagnosticLevel.Warning,
                    filePath: model.Path,
                    useResolvedReferenceRule: model.Resolved,
                    schemaName: model.SchemaName,
                    schemaItemType: model.SchemaItemType,
                    browseObjectProperties: model.Properties);
            }
        }
    }

    private sealed class Source : ProjectValueDataSourceBase<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>
    {
        private readonly object _lock = new();
        private readonly Dictionary<IProjectDependenciesSubTreeProvider, ProviderState> _stateByProvider = new();
        private readonly ImmutableArray<IProjectDependenciesSubTreeProvider> _subTreeProviders;
        private readonly IBroadcastBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> _broadcastBlock;
        private readonly IReceivableSourceBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> _publicBlock;

        private int _version;

        public Source(IProjectCommonServices commonServices, ImmutableArray<IProjectDependenciesSubTreeProvider> subTreeProviders)
            : base(commonServices, synchronousDisposal: false, registerDataSource: false)
        {
            _subTreeProviders = subTreeProviders;

            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>>(
                nameFormat: $"{nameof(LegacyDependencySubscriber)} broadcast block {{1}}");

            // Publish an empty snapshot so we don't block downstream consumers if subtree providers don't publish
            // dependencies, or if they delay for a while before doing so.
            _broadcastBlock.Post(new ProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>(
                ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty,
                Empty.ProjectValueVersions.Add(DataSourceKey, _version)));

            _publicBlock = _broadcastBlock.SafePublicize();

            // Hook events.
            foreach (IProjectDependenciesSubTreeProvider subtreeProvider in subTreeProviders)
            {
                subtreeProvider.DependenciesChanged += OnSubtreeProviderDependenciesChanged;
            }
        }

        private void OnSubtreeProviderDependenciesChanged(object? sender, DependenciesChangedEventArgs e)
        {
            if (!e.Changes.AddedNodes.Any() && !e.Changes.RemovedNodes.Any() || e.Token.IsCancellationRequested)
            {
                return;
            }

            lock (_lock)
            {
                ProviderState providerState = GetProviderState(e.Provider);

                Post(providerState.Update(e.Changes));
            }

            return;

            ProviderState GetProviderState(IProjectDependenciesSubTreeProvider provider)
            {
                if (!_stateByProvider.TryGetValue(provider, out ProviderState? state))
                {
                    state = new(e.Provider);

                    _stateByProvider.Add(provider, state);
                }

                return state;
            }

            void Post(ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> snapshot)
            {
                // Caller must ensure calls do not overlap
                _version++;
                bool accepted = _broadcastBlock.Post(new ProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>(
                    snapshot,
                    Empty.ProjectValueVersions.Add(DataSourceKey, _version)));

                System.Diagnostics.Debug.Assert(accepted, "LegacyDependencySubscriber posted a message that was not accepted");
            }
        }

        public override NamedIdentity DataSourceKey { get; } = new NamedIdentity(nameof(LegacyDependencySubscriber));

        public override IComparable DataSourceVersion => _version;

        public override IReceivableSourceBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> SourceBlock => _publicBlock;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe events.
                foreach (IProjectDependenciesSubTreeProvider subtreeProvider in _subTreeProviders)
                {
                    subtreeProvider.DependenciesChanged -= OnSubtreeProviderDependenciesChanged;
                }
            }

            base.Dispose(disposing);
        }
    }
}
