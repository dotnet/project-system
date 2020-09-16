// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="ProjectSystem.Query.ProjectModel.ICategory"/> based on an ID.
    /// </summary>
    internal class CategoryByIdDataProducer : CategoryDataProducer, IQueryDataProducer<IReadOnlyCollection<EntityIdentity>, IEntityValue>
    {
        private readonly IProjectService2 _projectService;

        public CategoryByIdDataProducer(ICategoryPropertiesAvailableStatus properties, IProjectService2 projectService)
            : base(properties)
        {
            Requires.NotNull(projectService, nameof(projectService));
            _projectService = projectService;
        }

        public async Task SendRequestAsync(QueryProcessRequest<IReadOnlyCollection<EntityIdentity>> request)
        {
            Requires.NotNull(request, nameof(request));

            foreach (var requestId in request.RequestData)
            {
                if (requestId.KeysCount == 3
                    && requestId.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string path)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string propertyPageName)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.CategoryName, out string categoryName))
                {
                    try
                    {
                        if (_projectService.GetLoadedProject(path) is UnconfiguredProject project
                            && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                            && projectCatalog.GetSchema(propertyPageName) is Rule rule)
                        {
                            // We need the category's index in order to populate the "Order" field of the query model.
                            // This requires that we do a linear traversal of the categories, even though we only care
                            // about one.
                            //
                            // TODO: if the "Order" property hasn't been requested, we can skip the linear traversal in
                            // favor of just looking it up by name.
                            foreach ((var index, var category) in rule.EvaluatedCategories.WithIndices())
                            {
                                if (StringComparers.CategoryNames.Equals(category.Name, categoryName))
                                {
                                    IEntityValue categoryValue = CreateCategoryValue(request.QueryExecutionContext.EntityRuntime, requestId, category, index);
                                    await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(categoryValue, request, ProjectModelZones.Cps));
                                }
                            }
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
