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
        private TreeItemOrderPropertyProvider latestTreeItemOrderPropertyProvider;

        [ImportingConstructor]
        public TreeItemOrderPropertyProviderSource(
            UnconfiguredProject project,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IOrderedSourceItemsDataSourceService orderedItemSource)
            : base(project.Services)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(orderedItemSource, nameof(orderedItemSource));

            _project = project;
            _orderedItemSource = orderedItemSource;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<IProjectTreePropertiesProvider>> targetBlock)
        {
            JoinUpstreamDataSources(_orderedItemSource);
            
            var providerProducerBlock = new TransformBlock<IProjectVersionedValue<IReadOnlyCollection<ProjectItemIdentity>>, IProjectVersionedValue<IProjectTreePropertiesProvider>>(
                orderedItems =>
                {
                    if (latestTreeItemOrderPropertyProvider?.OrderedItems != orderedItems.Value)
                    {
                        latestTreeItemOrderPropertyProvider = new TreeItemOrderPropertyProvider(orderedItems.Value, _project);
                    }

                    return new ProjectVersionedValue<IProjectTreePropertiesProvider>(latestTreeItemOrderPropertyProvider, orderedItems.DataSourceVersions);
                }, 
                new ExecutionDataflowBlockOptions() { NameFormat= "Ordered Tree Item Input: {1}" });

            providerProducerBlock.LinkTo(targetBlock, new DataflowLinkOptions() { PropagateCompletion = true });
            return _orderedItemSource.SourceBlock.LinkTo(providerProducerBlock, new DataflowLinkOptions() { PropagateCompletion = true });
        }
    }
}
