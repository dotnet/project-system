// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="IPropertyPageSnapshot"/> based on an ID.
    /// </summary>
    internal class PropertyPageByIdDataProducer : QueryDataByIdProducerBase
    {
        private readonly IPropertyPagePropertiesAvailableStatus _properties;
        private readonly IProjectService2 _projectService;

        public PropertyPageByIdDataProducer(IPropertyPagePropertiesAvailableStatus properties, IProjectService2 projectService)
        {
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNull(projectService, nameof(projectService));

            _properties = properties;
            _projectService = projectService;
        }

        protected override Task<IEntityValue?> TryCreateEntityOrNullAsync(IQueryExecutionContext queryExecutionContext, EntityIdentity id)
        {
            if (QueryProjectPropertiesContext.TryCreateFromEntityId(id, out QueryProjectPropertiesContext? propertiesContext)
                && id.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string? propertyPageName))
            {
                return PropertyPageDataProducer.CreatePropertyPageValueAsync(
                    queryExecutionContext,
                    id,
                    _projectService,
                    propertiesContext,
                    propertyPageName,
                    _properties);
            }

            return NullEntityValue;
            
        }
    }
}
