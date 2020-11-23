// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
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
    /// Handles retrieving a set of <see cref="IUIEditorMetadata"/> from a <see cref="ValueEditor"/> and reporting the
    /// results.
    /// </summary>
    /// <remarks>
    /// The <see cref="ValueEditor"/> comes from the parent <see cref="IUIPropertyEditor"/>
    /// </remarks>
    internal class UIEditorMetadataFromValueEditorProducer : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        private readonly IUIEditorMetadataPropertiesAvailableStatus _properties;

        public UIEditorMetadataFromValueEditorProducer(IUIEditorMetadataPropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            Requires.NotNull(request, nameof(request));

            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is ValueEditor editor)
            {
                try
                {
                    foreach (IEntityValue metadataValue in UIEditorMetadataProducer.CreateMetadataValues(request.RequestData, editor, _properties))
                    {
                        await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(metadataValue, request, ProjectModelZones.Cps));
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
