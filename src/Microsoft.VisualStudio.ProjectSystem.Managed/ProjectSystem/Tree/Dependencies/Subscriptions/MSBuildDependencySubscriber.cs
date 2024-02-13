// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

/// <summary>
/// Produces snapshots of dependencies within a project configuration slice, where those dependencies are sourced
/// from MSBuild items.
/// </summary>
/// <remarks>
/// <para>
/// Each dependency has a corresponding unresolved (from evaluation) and resolved (from build) MSBuild item.
/// The set of items supported by this subscriber is defined by <see cref="IMSBuildDependencyFactory"/> exports.
/// </para>
/// <para>
/// Because we operate per-slice, we cannot simply export an <see cref="IProjectValueDataSource{T}"/> as we do
/// for other data subscriptions. Instead, we have an unconfigured "subscriber" class that produces those data sources
/// for slices upon request.
/// </para>
/// </remarks>
[Export(typeof(IDependencySliceSubscriber))]
[AppliesTo(ProjectCapability.DependenciesTree)]
internal sealed class MSBuildDependencySubscriber : OnceInitializedOnceDisposed, IDependencySliceSubscriber
{
    [ImportMany]
    private readonly OrderPrecedenceImportCollection<IMSBuildDependencyFactory> _factories;
    private readonly UnconfiguredProject _unconfiguredProject;

    private (ImmutableArray<IMSBuildDependencyFactory> Factories, ImmutableHashSet<string> UnresolvedRuleNames, ImmutableHashSet<string> ResolvedRuleNames)? _state;

    [ImportingConstructor]
    public MSBuildDependencySubscriber(UnconfiguredProject unconfiguredProject)
    {
        _unconfiguredProject = unconfiguredProject;

        _factories = new OrderPrecedenceImportCollection<IMSBuildDependencyFactory>(
            projectCapabilityCheckProvider: unconfiguredProject);
    }

    protected override void Initialize()
    {
        // Capture the set of factories at this point and use that collection consistently from here on.
        // The imported values may change over time in response to dynamic project capabilities, which we don't handle.
        ImmutableArray<IMSBuildDependencyFactory> factories = _factories.ExtensionValues().ToImmutableArray();

        // Also capture the set of rule names from these factories, for later use.
        ImmutableHashSet<string> evaluationRuleNames = GetRuleNames(includeResolved: false);
        ImmutableHashSet<string> buildRuleNames = GetRuleNames(includeResolved: true);

        _state = (factories, evaluationRuleNames, buildRuleNames);

        return;

        ImmutableHashSet<string> GetRuleNames(bool includeResolved)
        {
            var builder = ImmutableHashSet.CreateBuilder(StringComparers.RuleNames);

            foreach (IMSBuildDependencyFactory factory in factories)
            {
                if (includeResolved)
                {
                    builder.Add(factory.ResolvedRuleName);
                }
                else
                {
                    builder.Add(factory.UnresolvedRuleName);
                }
            }

            return builder.ToImmutable();
        }
    }

    protected override void Dispose(bool disposing)
    {
    }

    public IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> Subscribe(ProjectConfigurationSlice slice, IActiveConfigurationSubscriptionSource source)
    {
        EnsureInitialized();

        Assumes.NotNull(_state);

        var subscriptions = _state.Value.Factories.Select(factory => (factory.CreateCollection(), factory.UnresolvedRuleName, factory.ResolvedRuleName)).ToImmutableArray();

        return new Source(_unconfiguredProject, source, subscriptions, _state.Value.UnresolvedRuleNames, _state.Value.ResolvedRuleNames);
    }

    /// <summary>
    /// A <see cref="IProjectValueDataSource{T}"/> for all MSBuild dependencies, scoped to a given project configuration slice.
    /// </summary>
    private sealed class Source : ChainedProjectValueDataSourceBase<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>
    {
        private readonly IActiveConfigurationSubscriptionSource _source;
        private readonly ImmutableArray<(MSBuildDependencyCollection Collection, string UnresolvedRuleName, string ResolvedRuleName)> _subscriptions;
        private readonly ImmutableHashSet<string> _evaluationRuleNames;
        private readonly ImmutableHashSet<string> _buildRuleNames;

        private ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> _dependenciesByGroupType = ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty;

        public Source(
            UnconfiguredProject unconfiguredProject,
            IActiveConfigurationSubscriptionSource source,
            ImmutableArray<(MSBuildDependencyCollection Collection, string UnresolvedRuleName, string ResolvedRuleName)> subscriptions,
            ImmutableHashSet<string> evaluationRuleNames,
            ImmutableHashSet<string> buildRuleNames)
            : base(unconfiguredProject, synchronousDisposal: false, registerDataSource: false)
        {
            _source = source;
            _subscriptions = subscriptions;
            _evaluationRuleNames = evaluationRuleNames;
            _buildRuleNames = buildRuleNames;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> targetBlock)
        {
            var transformBlock = DataflowBlockSlim.CreateTransformBlock<
                IProjectVersionedValue<IProjectSubscriptionUpdate>,
                IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>>(
                    transformFunction: u => u.Derive(Transform),
                    nameFormat: "MSBuild Dependency Merge {1}");

            return new DisposableBag
            {
                // Link evaluation data.
                _source.ProjectRuleSource.SourceBlock.LinkTo(transformBlock, DataflowOption.WithRuleNames(_evaluationRuleNames)),

                // Link joint data (evaluation and build).
                _source.JointRuleSource.SourceBlock.LinkTo(transformBlock, DataflowOption.WithJointRuleNames(_evaluationRuleNames, _buildRuleNames)),

                transformBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion),

                JoinUpstreamDataSources(_source.ProjectRuleSource, _source.JointRuleSource)
            };

            ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> Transform(IProjectSubscriptionUpdate update)
            {
                // Process updated project data, either from evaluation only, or joint (evaluation and build).

                // Broken design-time builds can produce updates containing no rule data.
                // Subscriptions assume their rules are available, so return early if we see no rule data.
                if (update.ProjectChanges.Count != 0)
                {
                    // Give each subscription a chance to process rule updates.
                    foreach ((MSBuildDependencyCollection collection, string unresolvedRuleName, string resolvedRuleName) in _subscriptions)
                    {
                        IProjectChangeDescription evaluationProjectChange = update.ProjectChanges[unresolvedRuleName];
                        IProjectChangeDescription? buildProjectChange = update.ProjectChanges.GetValueOrDefault(resolvedRuleName);

                        // NOTE there is a 1:1 relationship between subscription and dependency group type:
                        // - A subscription may only produce one type.
                        // - Two different factories may not produce the same type.

                        // TODO we should take this value from the project's own MSBuildProjectFullPath property, to ensure it is always in sync with dataflow data
                        string? projectFullPath = ContainingProject?.FullPath;
                        Assumes.NotNull(projectFullPath);

                        if (collection.TryUpdate(evaluationProjectChange, buildProjectChange, projectFullPath, out ImmutableArray<IDependency>? dependencies))
                        {
                            if (dependencies is null)
                            {
                                _dependenciesByGroupType = _dependenciesByGroupType.Remove(collection.DependencyGroupType);
                            }
                            else
                            {
                                _dependenciesByGroupType = _dependenciesByGroupType.SetItem(collection.DependencyGroupType, dependencies.Value);
                            }
                        }
                    }
                }

                return _dependenciesByGroupType;
            }
        }
    }
}
