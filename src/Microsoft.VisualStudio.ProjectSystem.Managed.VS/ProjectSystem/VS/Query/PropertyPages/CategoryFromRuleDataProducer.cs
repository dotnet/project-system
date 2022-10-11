// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="ICategory"/>s from an <see cref="IPropertyPageSnapshot"/>
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

        protected override async Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, ContextAndRuleProviderState providerState)
        {
            if (await providerState.ProjectState.GetMetadataVersionAsync() is (string versionKey, long versionNumber))
            {
                queryExecutionContext.ReportInputDataVersion(versionKey, versionNumber);
            }

            return CategoryDataProducer.CreateCategoryValues(queryExecutionContext, parent, providerState.Rule, _properties);
        }
    }
}
