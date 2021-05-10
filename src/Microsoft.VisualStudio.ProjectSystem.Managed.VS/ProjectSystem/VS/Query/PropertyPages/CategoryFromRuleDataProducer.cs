// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="ICategory"/>s from an <see cref="IPropertyPage"/>
    /// or <see cref="ILaunchProfile"/>.
    /// </summary>
    internal class CategoryFromRuleDataProducer : QueryDataFromProviderStateProducerBase<ContextAndRuleProviderState>
    {
        private readonly ICategoryPropertiesAvailableStatus _properties;

        public CategoryFromRuleDataProducer(ICategoryPropertiesAvailableStatus properties)
        {
            Requires.NotNull(properties, nameof(properties));
            _properties = properties;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext executionContext, IEntityValue parent, ContextAndRuleProviderState providerState)
        {
            (string versionKey, long versionNumber) = providerState.Cache.GetUnconfiguredProjectVersion();
            executionContext.ReportInputDataVersion(versionKey, versionNumber);

            return Task.FromResult(CategoryDataProducer.CreateCategoryValues(executionContext, parent, providerState.Rule, _properties));
        }
    }
}
