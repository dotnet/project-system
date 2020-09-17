// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IUIEditorMetadata"/> instances and populating the requested members.
    /// </summary>
    internal abstract class UIEditorMetadataProducer : QueryDataProducerBase<IEntityValue>
    {
        protected UIEditorMetadataProducer(IUIEditorMetadataPropertiesAvailableStatus properties)
        {
            Requires.NotNull(properties, nameof(properties));
            Properties = properties;
        }

        protected IUIEditorMetadataPropertiesAvailableStatus Properties { get; }

        protected IEntityValue CreateMetadataValue(IEntityRuntimeModel entityRuntime, NameValuePair metadata)
        {
            var newMetadata = new UIEditorMetadataValue(entityRuntime, new UIEditorMetadataPropertiesAvailableStatus());

            if (Properties.Name)
            {
                newMetadata.Name = metadata.Name;
            }

            if (Properties.Value)
            {
                newMetadata.Value = metadata.Value;
            }

            return newMetadata;
        }
    }
}
