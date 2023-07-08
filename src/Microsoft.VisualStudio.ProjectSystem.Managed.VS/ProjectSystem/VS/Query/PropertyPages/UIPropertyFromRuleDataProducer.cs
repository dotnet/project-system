// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IUIPropertySnapshot"/>s from an <see cref="IPropertyPageSnapshot"/>
    /// or <see cref="ILaunchProfile"/>.
    /// </summary>
    internal class UIPropertyFromRuleDataProducer : QueryDataFromProviderStateProducerBase<ContextAndRuleProviderState>
    {
        private readonly IUIPropertyPropertiesAvailableStatus _properties;

        public UIPropertyFromRuleDataProducer(IUIPropertyPropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        protected override async Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, ContextAndRuleProviderState providerState)
        {
            if (await providerState.ProjectState.GetMetadataVersionAsync() is (string versionKey, long versionNumber))
            {
                queryExecutionContext.ReportInputDataVersion(versionKey, versionNumber);
            }

            return UIPropertyDataProducer.CreateUIPropertyValues(queryExecutionContext, parent, providerState.ProjectState, providerState.PropertiesContext, providerState.Rule, _properties);
        }
    }
}
