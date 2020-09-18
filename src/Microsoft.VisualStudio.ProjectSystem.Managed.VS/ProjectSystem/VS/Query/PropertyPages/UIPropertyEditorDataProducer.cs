// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IUIPropertyEditor"/> instances and populating the requested members.
    /// </summary>
    internal static class UIPropertyEditorDataProducer
    {
        public static IEntityValue CreateEditorValue(IEntityValue entity, ValueEditor editor, IUIPropertyEditorPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(entity, nameof(entity));
            Requires.NotNull(editor, nameof(editor));

            var identity = new EntityIdentity(
                ((IEntityWithId)entity).Id,
                new KeyValuePair<string, string>[]
                {
                    new(ProjectModelIdentityKeys.EditorName, editor.EditorType)
                });

            return CreateEditorValue(entity.EntityRuntime, identity, editor, requestedProperties);
        }

        public static IEntityValue CreateEditorValue(IEntityRuntimeModel entityRuntime, EntityIdentity identity, ValueEditor editor, IUIPropertyEditorPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(editor, nameof(editor));
            var newEditorValue = new UIPropertyEditorValue(entityRuntime, identity, new UIPropertyEditorPropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                newEditorValue.Name = editor.EditorType;
            }

            ((IEntityValueFromProvider)newEditorValue).ProviderState = editor;

            return newEditorValue;
        }

        public static IEnumerable<IEntityValue> CreateEditorValues(IEntityValue requestData, Rule schema, string propertyName, IUIPropertyEditorPropertiesAvailableStatus properties)
        {
            BaseProperty? property = schema.GetProperty(propertyName);
            if (property is not null)
            {
                return createEditorValues();
            }

            return Enumerable.Empty<IEntityValue>();

            IEnumerable<IEntityValue> createEditorValues()
            {
                foreach (ValueEditor editor in property.ValueEditors)
                {
                    IEntityValue editorValue = CreateEditorValue(requestData, editor, properties);
                    yield return editorValue;
                }
            }
        }

        public static async Task<IEntityValue?> CreateEditorValueAsync(
            IEntityRuntimeModel entityRuntime,
            EntityIdentity requestId,
            IProjectService2 projectService,
            string projectPath,
            string propertyPageName,
            string propertyName,
            string editorName,
            IUIPropertyEditorPropertiesAvailableStatus properties)
        {
            if (projectService.GetLoadedProject(projectPath) is UnconfiguredProject project
                && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                && projectCatalog.GetSchema(propertyPageName) is Rule rule
                && rule.GetProperty(propertyName) is BaseProperty property
                && property.ValueEditors.FirstOrDefault(ed => string.Equals(ed.EditorType, editorName)) is ValueEditor editor)
            {
                IEntityValue editorValue = CreateEditorValue(entityRuntime, requestId, editor, properties);
            }

            return null;
        }
    }
}
