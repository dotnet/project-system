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
    /// Creates <see cref="IQueryDataProducer{TRequest, TResult}"/> instances that retrieve configuration dimension
    /// information (<see cref="IConfigurationDimension"/>).
    /// </summary>
    /// <remarks>
    /// Responsible for populating <see cref="IUIPropertyValue.ConfigurationDimensions"/>. See <see
    /// cref="ConfigurationDimensionDataProducer"/> and <see cref="ConfigurationDimensionFromPropertyDataProducer"/> for
    /// the important logic.
    /// </remarks>
    [QueryDataProvider(ConfigurationDimensionType.TypeName, ProjectModel.ModelName)]
    [RelationshipQueryDataProvider(UIPropertyValueType.TypeName, UIPropertyValueType.ConfigurationDimensionsPropertyName)]
    [QueryDataProviderZone(ProjectModelZones.Cps)]
    [Export(typeof(IQueryByRelationshipDataProvider))]
    internal class ConfigurationDimensionDataProvider : IQueryByRelationshipDataProvider
    {
        IQueryDataProducer<IEntityValue, IEntityValue> IQueryByRelationshipDataProvider.CreateQueryDataSource(IPropertiesAvailableStatus properties)
        {
            return new ConfigurationDimensionFromPropertyDataProducer((IConfigurationDimensionPropertiesAvailableStatus)properties);
        }
    }
}
