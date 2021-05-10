// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModelMethods.Actions;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal class SetLaunchProfilePropertyAction : QueryDataProducerBase<IEntityValue>, IQueryActionExecutor
    {
        private readonly SetLaunchProfilePropertyValue _executableStep;

        public SetLaunchProfilePropertyAction(SetLaunchProfilePropertyValue executableStep)
        {
            _executableStep = executableStep;
        }

        public Task OnRequestProcessFinishedAsync(IQueryProcessRequest request)
        {
            return ResultReceiver.OnRequestProcessFinishedAsync(request);
        }

        public async Task ReceiveResultAsync(QueryProcessResult<IEntityValue> result)
        {
            result.Request.QueryExecutionContext.CancellationToken.ThrowIfCancellationRequested();
            if (((IEntityValueFromProvider)result.Result).ProviderState is ContextAndRuleProviderState state)
            {
                var cache = state.Cache;
                if (await cache.GetSuggestedConfigurationAsync() is ProjectConfiguration configuration
                    && await cache.BindToRule(configuration, state.Rule.Name, state.Context) is IRule boundRule
                    && boundRule.GetProperty(_executableStep.PropertyName) is IProperty property)
                {
                    await property.SetValueAsync(_executableStep.Value);
                }
            }

            await ResultReceiver.ReceiveResultAsync(result);
        }
    }
}
