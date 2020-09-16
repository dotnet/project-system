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
    /// Handles retrieving a set of <see cref="IConfigurationDimension"/>s from an <see cref="ProjectSystem.Properties.IProperty"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="ProjectSystem.Properties.IProperty"/> comes from the parent <see cref="IUIPropertyValue"/>
    /// </remarks>
    internal class ConfigurationDimensionFromPropertyDataProducer : ConfigurationDimensionDataProducer, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        public ConfigurationDimensionFromPropertyDataProducer(IConfigurationDimensionPropertiesAvailableStatus properties)
            : base(properties)
        {
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            Requires.NotNull(request, nameof(request));
            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is (ProjectConfiguration configuration, ProjectSystem.Properties.IProperty property))
            {
                try
                {
                    foreach (KeyValuePair<string, string> dimension in configuration.Dimensions)
                    {
                        IEntityValue ProjectConfigurationDimension = CreateProjectConfigurationDimension(request.RequestData.EntityRuntime, dimension);
                        await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(ProjectConfigurationDimension, request, ProjectModelZones.Cps));
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
