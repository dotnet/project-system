// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="ISupportedValue"/>s from an <see cref="IUIPropertyValue"/>.
    /// </summary>
    internal class SupportedValueFromPropertyDataProducer : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        private readonly ISupportedValuePropertiesAvailableStatus _properties;

        public SupportedValueFromPropertyDataProducer(ISupportedValuePropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            Requires.NotNull(request, nameof(request));

            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is PropertyValueProviderState providerState
                && providerState.Property is ProjectSystem.Properties.IEnumProperty enumProperty)
            {
                try
                {
                    IEnumerable<IEntityValue> supportedValues = await SupportedValueDataProducer.CreateSupportedValuesAsync(request.RequestData.EntityRuntime, enumProperty, _properties);
                    foreach (IEntityValue supportedValue in supportedValues)
                    {
                        await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(supportedValue, request, ProjectModelZones.Cps));
                    }
                }
                catch (Exception ex)
                {
                    request.QueryExecutionContext.ReportError(ex);
                }
            }

            await ResultReceiver.OnRequestProcessFinishedAsync(request);
        }
    }
}
