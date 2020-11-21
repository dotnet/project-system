// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="ICategory"/> based on an ID.
    /// </summary>
    internal class CategoryByIdDataProducer : QueryDataByIdProducerBase<CategoryByIdDataProducer.KeyData>
    {
        private readonly ICategoryPropertiesAvailableStatus _properties;
        private readonly IProjectService2 _projectService;

        public CategoryByIdDataProducer(ICategoryPropertiesAvailableStatus properties, IProjectService2 projectService)
        {
            _properties = properties;
            _projectService = projectService;
        }

        protected override Task<IEntityValue?> TryCreateEntityOrNullAsync(IEntityRuntimeModel runtimeModel, EntityIdentity id, KeyData keyData)
        {
            return CategoryDataProducer.CreateCategoryValueAsync(
                runtimeModel,
                id,
                _projectService,
                keyData.ProjectPath,
                keyData.PropertyPageName,
                keyData.CategoryName,
                _properties);
        }

        protected override KeyData? TryExtactKeyDataOrNull(EntityIdentity requestId)
        {
            if (requestId.KeysCount == 3
                && requestId.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string projectPath)
                && requestId.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string propertyPageName)
                && requestId.TryGetValue(ProjectModelIdentityKeys.CategoryName, out string categoryName))
            {
                return new KeyData(projectPath, propertyPageName, categoryName);
            }

            return null;
        }
        internal class KeyData
        {
            public KeyData(string projectPath, string propertyPageName, string categoryName)
            {
                ProjectPath = projectPath;
                PropertyPageName = propertyPageName;
                CategoryName = categoryName;
            }

            public string ProjectPath { get; }
            public string PropertyPageName { get; }
            public string CategoryName { get; }
        }
    }
}
