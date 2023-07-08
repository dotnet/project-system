// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework.Actions;

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
                IProjectState projectState = state.ProjectState;
                if (await projectState.GetSuggestedConfigurationAsync() is ProjectConfiguration configuration
                    && await projectState.BindToRuleAsync(configuration, state.Rule.Name, state.PropertiesContext) is IRule boundRule
                    && boundRule.GetProperty(_executableStep.PropertyName) is IProperty property)
                {
                    await property.SetValueAsync(_executableStep.Value);

                    if (await projectState.GetDataVersionAsync(configuration) is (string versionKey, long versionNumber))
                    {
                        result.Request.QueryExecutionContext.ReportUpdatedDataVersion(versionKey, versionNumber);
                    }
                }
            }

            await ResultReceiver.ReceiveResultAsync(result);
        }
    }
}
