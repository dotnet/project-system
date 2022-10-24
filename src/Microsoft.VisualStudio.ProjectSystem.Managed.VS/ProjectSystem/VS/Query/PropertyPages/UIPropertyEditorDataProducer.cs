// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IUIPropertyEditorSnapshot"/> instances and populating the requested members.
    /// </summary>
    internal static class UIPropertyEditorDataProducer
    {
        public static IEntityValue CreateEditorValue(IQueryExecutionContext queryExecutionContext, IEntityValue parent, ValueEditor editor, IUIPropertyEditorPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(parent, nameof(parent));
            Requires.NotNull(editor, nameof(editor));

            var identity = new EntityIdentity(
                ((IEntityWithId)parent).Id,
                new KeyValuePair<string, string>[]
                {
                    new(ProjectModelIdentityKeys.EditorName, editor.EditorType)
                });

            return CreateEditorValue(queryExecutionContext, identity, editor, requestedProperties);
        }

        public static IEntityValue CreateEditorValue(IQueryExecutionContext queryExecutionContext, EntityIdentity identity, ValueEditor editor, IUIPropertyEditorPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(editor, nameof(editor));
            var newEditorValue = new UIPropertyEditorSnapshot(queryExecutionContext.EntityRuntime, identity, new UIPropertyEditorPropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                newEditorValue.Name = editor.EditorType;
            }

            ((IEntityValueFromProvider)newEditorValue).ProviderState = editor;

            return newEditorValue;
        }

        public static IEnumerable<IEntityValue> CreateEditorValues(IQueryExecutionContext queryExecutionContext, IEntityValue parent, Rule schema, string propertyName, IUIPropertyEditorPropertiesAvailableStatus properties)
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
                    IEntityValue editorValue = CreateEditorValue(queryExecutionContext, parent, editor, properties);
                    yield return editorValue;
                }

                if (property is StringProperty stringProperty)
                {
                    if (string.Equals(stringProperty.Subtype, "file", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return CreateEditorValue(queryExecutionContext, parent, new ValueEditor { EditorType = "FilePath" }, properties);
                    }
                    else if (
                        string.Equals(stringProperty.Subtype, "folder", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(stringProperty.Subtype, "directory", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return CreateEditorValue(queryExecutionContext, parent, new ValueEditor { EditorType = "DirectoryPath" }, properties);
                    }
                }
            }
        }

        public static async Task<IEntityValue?> CreateEditorValueAsync(
            IQueryExecutionContext queryExecutionContext,
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
                IEntityValue editorValue = CreateEditorValue(queryExecutionContext, requestId, editor, properties);
            }

            return null;
        }
    }
}
