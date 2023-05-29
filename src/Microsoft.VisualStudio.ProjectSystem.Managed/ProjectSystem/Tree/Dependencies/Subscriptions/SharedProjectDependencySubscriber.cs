// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

/// <summary>
/// Produces snapshots of shared projects within a project configuration slice.
/// </summary>
/// <remarks>
/// Because we operate per-slice, we cannot simply export an <see cref="IProjectValueDataSource{T}"/> as we do
/// for other data subscriptions. Instead, we have an unconfigured "subscriber" class that produces those data sources
/// for slices upon request.
/// </remarks>
[Export(typeof(IDependencySliceSubscriber))]
[AppliesTo(ProjectCapability.DependenciesTree + " & " + ProjectCapabilities.SharedProjectReferences)]
internal sealed class SharedProjectDependencySubscriber : IDependencySliceSubscriber
{
    private readonly UnconfiguredProject _unconfiguredProject;

    [ImportingConstructor]
    public SharedProjectDependencySubscriber(UnconfiguredProject unconfiguredProject)
    {
        _unconfiguredProject = unconfiguredProject;
    }

    public IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> Subscribe(ProjectConfigurationSlice slice, IActiveConfigurationSubscriptionSource source)
    {
        return new Source(_unconfiguredProject, source);
    }

    /// <summary>
    /// A <see cref="IProjectValueDataSource{T}"/> for shared project dependencies, scoped to a given project configuration slice.
    /// </summary>
    private sealed class Source : ChainedProjectValueDataSourceBase<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>
    {
        private static readonly ProjectTreeFlags s_flags =
            DependencyTreeFlags.ResolvedDependencyFlags +
            DependencyTreeFlags.ProjectDependency +
            DependencyTreeFlags.SharedProjectDependency +
            DependencyTreeFlags.SupportsBrowse +
            ProjectTreeFlags.FileSystemEntity;

        private readonly IActiveConfigurationSubscriptionSource _source;
        private readonly List<IDependency> _dependencies = new();

        public Source(UnconfiguredProject unconfiguredProject, IActiveConfigurationSubscriptionSource source)
            : base(unconfiguredProject, synchronousDisposal: false, registerDataSource: false)
        {
            _source = source;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>> targetBlock)
        {
            var transformBlock = DataflowBlockSlim.CreateTransformBlock<
                IProjectVersionedValue<IProjectSharedFoldersSnapshot>,
                IProjectVersionedValue<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>>(
                    transformFunction: u => u.Derive(Transform),
                    nameFormat: "Shared Project Dependencies Transform {1}");

            return new DisposableBag()
            {
                _source.SharedFoldersSource.SourceBlock.LinkTo(transformBlock, DataflowOption.PropagateCompletion),

                transformBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion),

                JoinUpstreamDataSources(_source.SharedFoldersSource)
            };
        }

        private ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> Transform(IProjectSharedFoldersSnapshot update)
        {
            // TODO allocate less when nothing important actually changes -- update, don't just remove and re-add

            ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> dependenciesByGroupType
                = ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty;

            _dependencies.Clear();

            // If no shared projects exist, return a singleton value immediately.
            if (update.Value.Count != 0)
            {
                // Note that shared projects are always resolved and never implicit.

                foreach (IProjectSharedFolder sharedFolder in update.Value)
                {
                    _dependencies.Add(new Dependency(
                        id: sharedFolder.ProjectPath,
                        caption: Path.GetFileNameWithoutExtension(sharedFolder.ProjectPath),
                        icon: KnownProjectImageMonikers.SharedProject,
                        flags: s_flags,
                        diagnosticLevel: DiagnosticLevel.None,
                        filePath: sharedFolder.ProjectPath,
                        useResolvedReferenceRule: true,
                        schemaName: ResolvedProjectReference.SchemaName,
                        schemaItemType: ProjectReference.PrimaryDataSourceItemType,
                        browseObjectProperties: null));
                }

                // We show shared projects in the same group as regular project references.
                dependenciesByGroupType = ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty.Add(
                    DependencyGroupTypes.Projects,
                    _dependencies.ToImmutableArray());
            }

            return dependenciesByGroupType;
        }
    }
}
