// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// MEF entry point that creates <see cref="IQueryDataProducer{TRequest, TResult}"/> instances to retrieve property
    /// value editor metadata (see <see cref="IUIEditorMetadataSnapshot"/>).
    /// </summary>
    /// <remarks>
    /// Responsible for populating Metadata. See <see
    /// cref="UIEditorMetadataFromValueEditorProducer"/> and <see cref="UIEditorMetadataProducer"/> for the important
    /// logic.
    /// </remarks>
    [QueryDataProvider(UIEditorMetadataType.TypeName, ProjectModel.ModelName)]
    [RelationshipQueryDataProvider(UIPropertyEditorType.TypeName, UIPropertyEditorType.MetadataPropertyName)]
    [QueryDataProviderZone(ProjectModelZones.Cps)]
    [Export(typeof(IQueryByRelationshipDataProvider))]
    internal class UIEditorMetadataDataProvider : IQueryByRelationshipDataProvider
    {
        IQueryDataProducer<IEntityValue, IEntityValue> IQueryByRelationshipDataProvider.CreateQueryDataSource(IPropertiesAvailableStatus properties)
        {
            return new UIEditorMetadataFromValueEditorProducer((IUIEditorMetadataPropertiesAvailableStatus)properties);
        }
    }
}
