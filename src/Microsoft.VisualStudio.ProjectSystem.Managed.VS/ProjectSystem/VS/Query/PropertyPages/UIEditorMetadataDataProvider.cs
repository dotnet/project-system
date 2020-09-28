// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// MEF entry point that creates <see cref="IQueryDataProducer{TRequest, TResult}"/> instances to retrieve property
    /// value editor metadata (see <see cref="IUIEditorMetadata"/>).
    /// </summary>
    /// <remarks>
    /// Responsible for populating <see cref="IUIPropertyEditor.Metadata"/>. See <see
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
