// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Order;

/// <summary>
/// Setup the dataflow for a provider that updates solution tree item properties with
/// display order metadata derived from <see cref="IOrderedSourceItemsDataSourceService"/>.
/// </summary>
[Export(typeof(IProjectTreePropertiesProviderDataSource))]
[AppliesTo(ProjectCapability.SortByDisplayOrder + " & " + ProjectCapability.EditableDisplayOrder)]
internal class TreeItemOrderPropertyProviderSource : ChainedProjectValueDataSourceBase<IProjectTreePropertiesProvider>, IProjectTreePropertiesProviderDataSource
{
    private readonly UnconfiguredProject _project;
    private readonly IOrderedSourceItemsDataSourceService _orderedItemSource;

    [ImportingConstructor]
    public TreeItemOrderPropertyProviderSource(
        UnconfiguredProject project,
        [Import(ExportContractNames.Scopes.UnconfiguredProject)] IOrderedSourceItemsDataSourceService orderedItemSource)
        : base(project)
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
