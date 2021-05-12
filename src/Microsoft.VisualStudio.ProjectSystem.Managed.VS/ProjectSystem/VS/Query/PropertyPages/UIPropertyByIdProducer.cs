// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="IUIProperty"/> based on an ID.
    /// </summary>
    internal class UIPropertyByIdProducer : QueryDataByIdProducerBase
    { 
        private readonly IUIPropertyPropertiesAvailableStatus _properties;
        private readonly IProjectService2 _projectService;
        private readonly IProjectStateProvider _projectStateProvider;

        public UIPropertyByIdProducer(IUIPropertyPropertiesAvailableStatus properties, IProjectService2 projectService, IProjectStateProvider projectStateProvider)
        {
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNull(projectService, nameof(projectService));
            _properties = properties;
            _projectService = projectService;
            _projectStateProvider = projectStateProvider;
        }

        protected override Task<IEntityValue?> TryCreateEntityOrNullAsync(IQueryExecutionContext queryExecutionContext, EntityIdentity id)
        {
            if (QueryProjectPropertiesContext.TryCreateFromEntityId(id, out QueryProjectPropertiesContext? propertiesContext)
                && id.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string propertyPageName)
                && id.TryGetValue(ProjectModelIdentityKeys.UIPropertyName, out string propertyName))
            {
                return UIPropertyDataProducer.CreateUIPropertyValueAsync(
                    queryExecutionContext,
                    id,
                    _projectService,
                    _projectStateProvider,
                    propertiesContext,
                    propertyPageName,
                    propertyName,
                    _properties);
            }

            return NullEntityValue;   
        }
    }
}
