// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="IUIProperty"/> based on an ID.
    /// </summary>
    internal class UIPropertyByIdProducer : QueryDataByIdProducerBase<UIPropertyByIdProducer.KeyData>
    { 
        private readonly IUIPropertyPropertiesAvailableStatus _properties;
        private readonly IProjectService2 _projectService;
        private readonly IPropertyPageQueryCacheProvider _queryCacheProvider;

        public UIPropertyByIdProducer(IUIPropertyPropertiesAvailableStatus properties, IProjectService2 projectService, IPropertyPageQueryCacheProvider queryCacheProvider)
        {
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNull(projectService, nameof(projectService));
            _properties = properties;
            _projectService = projectService;
            _queryCacheProvider = queryCacheProvider;
        }

        protected override Task<IEntityValue?> TryCreateEntityOrNullAsync(IEntityRuntimeModel runtimeModel, EntityIdentity id, KeyData keyData)
        {
            return UIPropertyDataProducer.CreateUIPropertyValueAsync(
                runtimeModel,
                id,
                _projectService,
                _queryCacheProvider,
                keyData.ProjectPath,
                keyData.PropertyPageName,
                keyData.PropertyName,
                _properties);
        }

        protected override KeyData? TryExtactKeyDataOrNull(EntityIdentity requestId)
        {
            if (requestId.KeysCount == 3
                && requestId.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string path)
                && requestId.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string propertyPageName)
                && requestId.TryGetValue(ProjectModelIdentityKeys.UIPropertyName, out string propertyName))
            {
                return new KeyData(path, propertyPageName, propertyName);
            }

            return null;
        }

        internal sealed class KeyData
        {
            public KeyData(string projectPath, string propertyPageName, string propertyName)
            {
                ProjectPath = projectPath;
                PropertyPageName = propertyPageName;
                PropertyName = propertyName;
            }

            public string ProjectPath { get; }
            public string PropertyPageName { get; }
            public string PropertyName { get; }
        }
    }
}
