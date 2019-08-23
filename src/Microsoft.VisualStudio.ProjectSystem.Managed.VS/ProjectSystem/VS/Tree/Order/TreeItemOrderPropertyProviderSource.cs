// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Order
{
    /// <summary>
    /// Setup the dataflow for a provider that updates solution tree item properties with 
    /// display order metadata derived from IOrderedSourceItemsDataSourceService
    /// </summary>
    [Export(typeof(IProjectTreePropertiesProviderDataSource))]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class TreeItemOrderPropertyProviderSource : ChainedProjectValueDataSourceBase<IProjectTreePropertiesProvider>, IProjectTreePropertiesProviderDataSource
    {
        private readonly UnconfiguredProject _project;
        private readonly IOrderedSourceItemsDataSourceService _orderedItemSource;

        [ImportingConstructor]
        public TreeItemOrderPropertyProviderSource(
            UnconfiguredProject project,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IOrderedSourceItemsDataSourceService orderedItemSource)
            : base(project.Services)
        {
            _project = project;
            _orderedItemSource = orderedItemSource;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<IProjectTreePropertiesProvider>> targetBlock)
        {
            JoinUpstreamDataSources(_orderedItemSource);

            IPropagatorBlock<IProjectVersionedValue<IReadOnlyCollection<ProjectItemIdentity>>, IProjectVersionedValue<IProjectTreePropertiesProvider>> providerProducerBlock = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<IReadOnlyCollection<ProjectItemIdentity>>, IProjectVersionedValue<IProjectTreePropertiesProvider>>(
                orderedItems =>
                {
                    return new ProjectVersionedValue<IProjectTreePropertiesProvider>(new TreeItemOrderPropertyProvider(orderedItems.Value, _project), orderedItems.DataSourceVersions);
                },
                new ExecutionDataflowBlockOptions() { NameFormat = "Ordered Tree Item Input: {1}" });

            providerProducerBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion);
            return _orderedItemSource.SourceBlock.LinkTo(providerProducerBlock, DataflowOption.PropagateCompletion);
        }
    }
}
