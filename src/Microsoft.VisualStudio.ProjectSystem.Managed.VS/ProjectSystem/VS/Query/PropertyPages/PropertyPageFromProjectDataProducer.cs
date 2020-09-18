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
    /// Handles retrieving a set of <see cref="IPropertyPage"/>s from an <see cref="IProject"/>.
    /// </summary>
    internal class PropertyPageFromProjectDataProducer : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        private readonly IPropertyPagePropertiesAvailableStatus _properties;

        public PropertyPageFromProjectDataProducer(IPropertyPagePropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            Requires.NotNull(request, nameof(request));

            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is UnconfiguredProject project)
            {
                try
                {
                    IEnumerable<IEntityValue> propertyPageValues = await PropertyPageDataProducer.CreatePropertyPageValuesAsync(request.RequestData, project, _properties);
                    foreach (IEntityValue propertyPageValue in propertyPageValues)
                    {
                        await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(propertyPageValue, request, ProjectModelZones.Cps));
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
