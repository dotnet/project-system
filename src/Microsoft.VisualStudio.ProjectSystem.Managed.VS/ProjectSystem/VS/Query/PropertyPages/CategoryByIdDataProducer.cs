// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="ICategory"/> based on an ID.
    /// </summary>
    internal class CategoryByIdDataProducer : QueryDataByIdProducerBase
    {
        private readonly ICategoryPropertiesAvailableStatus _properties;
        private readonly IProjectService2 _projectService;

        public CategoryByIdDataProducer(ICategoryPropertiesAvailableStatus properties, IProjectService2 projectService)
        {
            _properties = properties;
            _projectService = projectService;
        }

        protected override Task<IEntityValue?> TryCreateEntityOrNullAsync(IQueryExecutionContext queryExecutionContext, EntityIdentity id)
        {
            if (id.KeysCount == 3
                && id.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string? projectPath)
                && id.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string? propertyPageName)
                && id.TryGetValue(ProjectModelIdentityKeys.CategoryName, out string? categoryName))
            {
                return CategoryDataProducer.CreateCategoryValueAsync(
                    queryExecutionContext,
                    id,
                    _projectService,
                    projectPath,
                    propertyPageName,
                    categoryName,
                    _properties);
            }

            return NullEntityValue;
        }
    }
}
