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
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IUIProperty"/>s from an <see cref="IPropertyPage"/>.
    /// </summary>
    internal class UIPropertyFromPropertyPageDataProducer : UIPropertyDataProducer, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        public UIPropertyFromPropertyPageDataProducer(IUIPropertyPropertiesAvailableStatus properties)
            : base(properties)
        {
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            Requires.NotNull(request, nameof(request));

            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is (IPropertyPageQueryCache context, Rule rule))
            {
                try
                {
                    foreach ((var index, var property) in rule.Properties.WithIndices())
                    {
                        if (property.Visible)
                        {
                            IEntityValue propertyValue = await CreateUIPropertyValueAsync(request.RequestData, context, property, index);
                            await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(propertyValue, request, ProjectModelZones.Cps));
                        }
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
