// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IUIPropertyEditor"/>s from an <see cref="IUIProperty"/>.
    /// </summary>
    internal class UIPropertyEditorFromUIPropertyDataProducer : QueryDataFromProviderStateProducerBase<PropertyProviderState>
    {
        private readonly IUIPropertyEditorPropertiesAvailableStatus _properties;

        public UIPropertyEditorFromUIPropertyDataProducer(IUIPropertyEditorPropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, PropertyProviderState providerState)
        {
            return Task.FromResult(UIPropertyEditorDataProducer.CreateEditorValues(queryExecutionContext, parent, providerState.ContainingRule, providerState.PropertyName, _properties));
        }
    }
}
