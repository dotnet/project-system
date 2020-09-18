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
    /// Handles retrieving an <see cref="ICategory"/> based on an ID.
    /// </summary>
    internal class CategoryByIdDataProducer : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IReadOnlyCollection<EntityIdentity>, IEntityValue>
    {
        private readonly IProjectService2 _projectService;
        private readonly ICategoryPropertiesAvailableStatus _properties;

        public CategoryByIdDataProducer(ICategoryPropertiesAvailableStatus properties, IProjectService2 projectService)
        {
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNull(projectService, nameof(projectService));

            _properties = properties;
            _projectService = projectService;

        }

        public async Task SendRequestAsync(QueryProcessRequest<IReadOnlyCollection<EntityIdentity>> request)
        {
            Requires.NotNull(request, nameof(request));

            foreach (EntityIdentity requestId in request.RequestData)
            {
                if (requestId.KeysCount == 3
                    && requestId.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string projectPath)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string propertyPageName)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.CategoryName, out string categoryName))
                {
                    try
                    {
                        IEntityValue? categoryValue = await CategoryDataProducer.CreateCategoryValueAsync(
                            request.QueryExecutionContext.EntityRuntime,
                            requestId,
                            _projectService,
                            projectPath,
                            propertyPageName,
                            categoryName,
                            _properties);

                        if (categoryValue != null)
                        {
                            await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(categoryValue, request, ProjectModelZones.Cps));
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
