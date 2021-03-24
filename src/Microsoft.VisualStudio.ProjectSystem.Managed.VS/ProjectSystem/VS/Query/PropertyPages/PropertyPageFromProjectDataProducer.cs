// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IPropertyPage"/>s from an <see cref="IProject"/>.
    /// </summary>
    internal class PropertyPageFromProjectDataProducer : QueryDataFromProviderStateProducerBase<UnconfiguredProject>
    {
        private readonly IPropertyPagePropertiesAvailableStatus _properties;
        private readonly IPropertyPageQueryCacheProvider _queryCacheProvider;

        public PropertyPageFromProjectDataProducer(IPropertyPagePropertiesAvailableStatus properties, IPropertyPageQueryCacheProvider queryCacheProvider)
        {
            _properties = properties;
            _queryCacheProvider = queryCacheProvider;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext executionContext, IEntityValue parent, UnconfiguredProject providerState)
        {
            executionContext.ReportProjectVersion(providerState);

            return PropertyPageDataProducer.CreatePropertyPageValuesAsync(executionContext, parent, providerState, _queryCacheProvider, _properties);
        }
    }
}
