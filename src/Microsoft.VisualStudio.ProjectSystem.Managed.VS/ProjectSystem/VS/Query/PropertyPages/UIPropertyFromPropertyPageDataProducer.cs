// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IUIProperty"/>s from an <see cref="IPropertyPage"/>.
    /// </summary>
    internal class UIPropertyFromPropertyPageDataProducer : QueryDataFromProviderStateProducerBase<PropertyPageProviderState>
    {
        private readonly IUIPropertyPropertiesAvailableStatus _properties;

        public UIPropertyFromPropertyPageDataProducer(IUIPropertyPropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IEntityValue parent, PropertyPageProviderState providerState)
        {
            return Task.FromResult(UIPropertyDataProducer.CreateUIPropertyValues(parent, providerState.Cache, providerState.Rule, providerState.DebugChildRules, _properties));
        }
    }
}
