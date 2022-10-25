// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IUIPropertyValueSnapshot"/>s from an <see cref="IUIPropertySnapshot"/>.
    /// </summary>
    internal class UIPropertyValueFromUIPropertyDataProducer : QueryDataFromProviderStateProducerBase<PropertyProviderState>
    {
        private readonly IUIPropertyValuePropertiesAvailableStatus _properties;

        public UIPropertyValueFromUIPropertyDataProducer(IUIPropertyValuePropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, PropertyProviderState providerState)
        {
            return UIPropertyValueDataProducer.CreateUIPropertyValueValuesAsync(
                queryExecutionContext,
                parent,
                providerState.ProjectState,
                providerState.ContainingRule,
                providerState.PropertiesContext,
                providerState.PropertyName,
                _properties);
        }
    }
}
