// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Creates <see cref="IQueryDataProducer{TRequest, TResult}"/> instances that retrieve property value information
    /// (<see cref="IUIPropertyValueSnapshot"/>).
    /// </summary>
    /// <remarks>
    /// Responsible for populating <see cref="IUIPropertySnapshot.Values"/>.
    /// </remarks>
    [QueryDataProvider(UIPropertyValueType.TypeName, ProjectModel.ModelName)]
    [RelationshipQueryDataProvider(UIPropertyType.TypeName, UIPropertyType.ValuesPropertyName)]
    [QueryDataProviderZone(ProjectModelZones.Cps)]
    [Export(typeof(IQueryByRelationshipDataProvider))]
    internal class UIPropertyValueDataProvider : IQueryByRelationshipDataProvider
    {
        public IQueryDataProducer<IEntityValue, IEntityValue> CreateQueryDataSource(IPropertiesAvailableStatus properties)
        {
            return new UIPropertyValueFromUIPropertyDataProducer((IUIPropertyValuePropertiesAvailableStatus)properties);
        }
    }
}
