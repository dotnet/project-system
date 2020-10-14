// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Text;

using GuidCollectionProjectValue = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<System.Collections.Immutable.IImmutableList<System.Guid>>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web
{
    /// <summary>
    ///     Provides project type <see cref="Guid"/> instances from the ProjectTypeGuids property.
    /// </summary>
    [Export]
    internal class ProjectTypeGuidsDataSource : ChainedProjectValueDataSourceBase<IImmutableList<Guid>>
    {
        private readonly IProjectSnapshotService _snapshotService;

        [ImportingConstructor]
        public ProjectTypeGuidsDataSource(UnconfiguredProject project, IProjectSnapshotService snapshotService)
            : base(project, synchronousDisposal: true)
        {
            ItemTypeGuidProviders = new OrderPrecedenceImportCollection<IItemTypeGuidProvider>(projectCapabilityCheckProvider: project);

            _snapshotService = snapshotService;
        }

        [ImportMany]
        private OrderPrecedenceImportCollection<IItemTypeGuidProvider> ItemTypeGuidProviders { get; }

        public async Task<IImmutableList<Guid>> GetProjectTypeGuids()
        {
            using (JoinableCollection.Join())
            {
                GuidCollectionProjectValue snapshot = await SourceBlock.ReceiveAsync();

                return snapshot.Value;
            }
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<GuidCollectionProjectValue> targetBlock)
        {
            JoinUpstreamDataSources(_snapshotService);

            DisposableValue<ISourceBlock<GuidCollectionProjectValue>> block = 
                _snapshotService.SourceBlock.Transform(
                    snapshot => snapshot.Derive(CreateSnapshot));

            block.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return block;
        }

        private IImmutableList<Guid> CreateSnapshot(IProjectSnapshot snapshot)
        {
            string propertyValue = snapshot.ProjectInstance.GetPropertyValue(ConfigurationGeneral.ProjectTypeGuidsProperty);

            // Legacy reads ProjectTypeGuids via evaluation for IVsAggregateProject.GetAggregateProjectTypeGuids
            // and so therefore we do to. Order has meaning and matters.

            ImmutableList<Guid>.Builder builder = ImmutableList.CreateBuilder<Guid>();
            foreach (string value in new LazyStringSplit(propertyValue, ';'))
            {
                if (Guid.TryParse(value, out Guid result))
                {
                    builder.Add(result);
                }
            }

            if (builder.Count == 0 && TryGetDefaultProjectType(out Guid? defaultProjectType))
            {   
                // We always return at least the default
                builder.Add(defaultProjectType.Value);
            }

            return builder.ToImmutable();
        }

        private bool TryGetDefaultProjectType([NotNullWhen(true)]out Guid? projectType)
        {
            IItemTypeGuidProvider? provider = ItemTypeGuidProviders.FirstOrDefault()?.Value;
            if (provider != null)
            {
                projectType = provider.ProjectTypeGuid;
                return true;
            }

            projectType = null;
            return false;
        }
    }
}
