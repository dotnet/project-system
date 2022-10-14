// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="ISupportedValueSnapshot"/>s from an <see cref="IUIPropertyValueSnapshot"/>.
    /// </summary>
    internal class SupportedValueFromPropertyDataProducer : QueryDataFromProviderStateProducerBase<PropertyValueProviderState>
    {
        private readonly ISupportedValuePropertiesAvailableStatus _properties;

        public SupportedValueFromPropertyDataProducer(ISupportedValuePropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, PropertyValueProviderState providerState)
        {
            return SupportedValueDataProducer.CreateSupportedValuesAsync(parent, providerState.Property, _properties);
        }
    }
}
