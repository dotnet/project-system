// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IUIProperty"/>s from an <see cref="IPropertyPage"/>.
    /// </summary>
    internal class UIPropertyFromPropertyPageDataProducer : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        private readonly IUIPropertyPropertiesAvailableStatus _properties;

        public UIPropertyFromPropertyPageDataProducer(IUIPropertyPropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            Requires.NotNull(request, nameof(request));

            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is (IPropertyPageQueryCache context, Rule rule))
            {
                try
                {
                    foreach (IEntityValue propertyValue in UIPropertyDataProducer.CreateUIPropertyValues(request.RequestData, context, rule, _properties))
                    {
                        await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(propertyValue, request, ProjectModelZones.Cps));
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
