// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the boilerplate of retrieving a set of <see cref="IEntityValue"/>s based on the
    /// state associated with the parent <see cref="IEntityValue"/>.
    /// </summary>
    internal abstract class QueryDataFromProviderStateProducerBase<T> : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is T providerState)
            {
                try
                {
                    foreach (IEntityValue categoryValue in await CreateValuesAsync(request.QueryExecutionContext, request.RequestData, providerState))
                    {
                        await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(categoryValue, request, ProjectModelZones.Cps));
                    }
                }
                catch (Exception ex)
                {
                    request.QueryExecutionContext.ReportError(ex);
                }
            }

            await ResultReceiver.OnRequestProcessFinishedAsync(request);
        }

        /// <summary>
        /// Given the <paramref name="parent"/> entity and the associated <paramref name="providerState"/>,
        /// returns a set of child entities.
        /// </summary>
        protected abstract Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, T providerState);
    }
}
