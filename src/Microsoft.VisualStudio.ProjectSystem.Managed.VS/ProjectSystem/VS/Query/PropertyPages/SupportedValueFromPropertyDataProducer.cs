// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
    internal class SupportedValueFromPropertyDataProducer : SupportedValueDataProducer, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        public SupportedValueFromPropertyDataProducer(ISupportedValuePropertiesAvailableStatus properties)
            : base(properties)
        {
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            Requires.NotNull(request, nameof(request));
            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is (ProjectConfiguration _, ProjectSystem.Properties.IEnumProperty enumProperty))
            {
                foreach (var value in await enumProperty.GetAdmissibleValuesAsync())
                {
                    IEntityValue projectConfigurationDimension = CreateSupportedValue(request.RequestData.EntityRuntime, value);
                    await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(projectConfigurationDimension, request, ProjectModelZones.Cps));
                }
            }

            await ResultReceiver.OnRequestProcessFinishedAsync(request);
        }
    }
}
