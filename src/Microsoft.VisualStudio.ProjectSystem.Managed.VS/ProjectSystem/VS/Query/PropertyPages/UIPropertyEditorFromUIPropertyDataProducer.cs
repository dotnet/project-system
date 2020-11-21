// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IUIPropertyEditor"/>s from an <see cref="IUIProperty"/>.
    /// </summary>
    internal class UIPropertyEditorFromUIPropertyDataProducer : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        private readonly IUIPropertyEditorPropertiesAvailableStatus _properties;

        public UIPropertyEditorFromUIPropertyDataProducer(IUIPropertyEditorPropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            Requires.NotNull(request, nameof(request));
            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is PropertyProviderState propertyState)
            {
                try
                {
                    foreach (IEntityValue propertyEditor in UIPropertyEditorDataProducer.CreateEditorValues(request.RequestData, propertyState.ContainingRule, propertyState.PropertyName, _properties))
                    {
                        await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(propertyEditor, request, ProjectModelZones.Cps));
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
