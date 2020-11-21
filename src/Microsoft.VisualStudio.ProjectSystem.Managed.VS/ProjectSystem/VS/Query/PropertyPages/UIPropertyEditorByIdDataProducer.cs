// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="IUIPropertyEditor"/> base on an ID.
    /// </summary>
    internal class UIPropertyEditorByIdDataProducer : QueryDataByIdProducerBase<UIPropertyEditorByIdDataProducer.KeyData>
    {
        private readonly IUIPropertyEditorPropertiesAvailableStatus _properties;
        private readonly IProjectService2 _projectService;

        public UIPropertyEditorByIdDataProducer(IUIPropertyEditorPropertiesAvailableStatus properties, IProjectService2 projectService)
        {
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNull(projectService, nameof(projectService));
            _properties = properties;
            _projectService = projectService;
        }

        protected override Task<IEntityValue?> TryCreateEntityOrNullAsync(IEntityRuntimeModel runtimeModel, EntityIdentity id, KeyData keyData)
        {
            return UIPropertyEditorDataProducer.CreateEditorValueAsync(
                runtimeModel,
                id,
                _projectService,
                keyData.ProjectPath,
                keyData.PropertyPageName,
                keyData.PropertyName,
                keyData.EditorName,
                _properties);
        }

        protected override KeyData? TryExtactKeyDataOrNull(EntityIdentity requestId)
        {
            if (requestId.KeysCount == 4
                && requestId.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string path)
                && requestId.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string propertyPageName)
                && requestId.TryGetValue(ProjectModelIdentityKeys.UIPropertyName, out string propertyName)
                && requestId.TryGetValue(ProjectModelIdentityKeys.EditorName, out string editorName))
            {
                return new KeyData(path, propertyPageName, propertyName, editorName);
            }

            return null;
        }

        internal sealed class KeyData
        {
            public KeyData(string projectPath, string propertyPageName, string propertyName, string editorName)
            {
                ProjectPath = projectPath;
                PropertyPageName = propertyPageName;
                PropertyName = propertyName;
                EditorName = editorName;
            }

            public string ProjectPath { get; }
            public string PropertyPageName { get; }
            public string PropertyName { get; }
            public string EditorName { get; }
        }
    }
}
