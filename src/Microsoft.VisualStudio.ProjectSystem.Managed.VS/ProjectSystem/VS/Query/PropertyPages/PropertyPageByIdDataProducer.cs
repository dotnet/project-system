// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="IPropertyPage"/> based on an ID.
    /// </summary>
    internal class PropertyPageByIdDataProducer : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IReadOnlyCollection<EntityIdentity>, IEntityValue>
    {
        private readonly IProjectService2 _projectService;
        private readonly IPropertyPagePropertiesAvailableStatus _properties;

        public PropertyPageByIdDataProducer(IPropertyPagePropertiesAvailableStatus properties, IProjectService2 projectService)
        {
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNull(projectService, nameof(projectService));

            _projectService = projectService;
            _properties = properties;
        }

        public async Task SendRequestAsync(QueryProcessRequest<IReadOnlyCollection<EntityIdentity>> request)
        {
            Requires.NotNull(request, nameof(request));

            foreach (EntityIdentity requestId in request.RequestData)
            {
                if (requestId.KeysCount == 2
                    && requestId.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string path)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string propertyPageName))
                {
                    try
                    {
                        IEntityValue? propertyPageValue = await PropertyPageDataProducer.CreatePropertyPageValueAsync(
                            request.QueryExecutionContext.EntityRuntime,
                            requestId,
                            _projectService,
                            path,
                            propertyPageName,
                            _properties);

                        if (propertyPageValue is not null)
                        {
                            await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(propertyPageValue, request, ProjectModelZones.Cps));
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
    }
}
