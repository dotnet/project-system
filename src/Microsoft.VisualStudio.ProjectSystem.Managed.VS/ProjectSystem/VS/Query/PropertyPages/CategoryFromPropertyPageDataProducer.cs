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
    /// Handles retrieving a set of <see cref="ICategory"/>s from an <see cref="IPropertyPage"/>.
    /// </summary>
    internal class CategoryFromPropertyPageDataProducer : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        private readonly ICategoryPropertiesAvailableStatus _properties;

        public CategoryFromPropertyPageDataProducer(ICategoryPropertiesAvailableStatus properties)
        {
            Requires.NotNull(properties, nameof(properties));
            _properties = properties;
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is (IPropertyPageQueryCache _, Rule rule))
            {
                try
                {
                    foreach (IEntityValue categoryValue in CategoryDataProducer.CreateCategoryValues(request.RequestData, rule, _properties))
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
    }
}
