// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IUIPropertyEditor"/> instances and populating the requested members.
    /// </summary>
    internal abstract class UIPropertyEditorDataProducer : QueryDataProducerBase<IEntityValue>
    {
        public UIPropertyEditorDataProducer(IUIPropertyEditorPropertiesAvailableStatus properties)
        {
            Requires.NotNull(properties, nameof(properties));
            Properties = properties;
        }

        protected IUIPropertyEditorPropertiesAvailableStatus Properties { get; }

        protected async Task<IEntityValue> CreateEditorValueAsync(IEntityValue entity, ValueEditor editor)
        {
            Requires.NotNull(entity, nameof(entity));
            Requires.NotNull(editor, nameof(editor));

            var identity = new EntityIdentity(
                ((IEntityWithId)entity).Id,
                new KeyValuePair<string, string>[]
                {
                        new(ProjectModelIdentityKeys.EditorName, editor.EditorType)
                });

            return await CreateEditorValueAsync(entity.EntityRuntime, identity, editor);
        }

        protected Task<IEntityValue> CreateEditorValueAsync(IEntityRuntimeModel entityRuntime, EntityIdentity identity, ValueEditor editor)
        {
            Requires.NotNull(editor, nameof(editor));
            var newEditorValue = new UIPropertyEditorValue(entityRuntime, identity, new UIPropertyEditorPropertiesAvailableStatus());

            if (Properties.Name)
            {
                newEditorValue.Name = editor.EditorType;
            }

            ((IEntityValueFromProvider)newEditorValue).ProviderState = editor;

            return Task.FromResult<IEntityValue>(newEditorValue);
        }
    }
}
