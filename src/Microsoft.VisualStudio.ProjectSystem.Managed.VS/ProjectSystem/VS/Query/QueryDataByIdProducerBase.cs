// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the boilerplate of retrieving an <see cref="IEntityValue"/> based on an ID.
    /// </summary>
    internal abstract class QueryDataByIdProducerBase : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IReadOnlyCollection<EntityIdentity>, IEntityValue>
    {
        protected static readonly Task<IEntityValue?> NullEntityValue = Task.FromResult<IEntityValue?>(null);

        public async Task SendRequestAsync(QueryProcessRequest<IReadOnlyCollection<EntityIdentity>> request)
        {
            if (request.RequestData is not null)
            {
                foreach (EntityIdentity requestId in request.RequestData)
                {
                    try
                    {
                        IEntityValue? entityValue = await TryCreateEntityOrNullAsync(request.QueryExecutionContext, requestId);
                        if (entityValue is not null)
                        {
                            await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(entityValue, request, ProjectModelZones.Cps));
                        }
                    }
                    catch (Exception ex)
                    {
                        request.QueryExecutionContext.ReportError(ex);
                    }
                }
            }

            await ResultReceiver.OnRequestProcessFinishedAsync(request);
        }

        protected abstract Task<IEntityValue?> TryCreateEntityOrNullAsync(IQueryExecutionContext queryExecutionContext, EntityIdentity id);
    }
}
