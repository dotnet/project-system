// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IUIEditorMetadataSnapshot"/> instances and populating the requested members.
    /// </summary>
    internal static class UIEditorMetadataProducer
    {
        public static IEntityValue CreateMetadataValue(IEntityRuntimeModel runtimeModel, NameValuePair metadata, IUIEditorMetadataPropertiesAvailableStatus requestedProperties)
        {
            var newMetadata = new UIEditorMetadataSnapshot(runtimeModel, new UIEditorMetadataPropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                newMetadata.Name = metadata.Name;
            }

            if (requestedProperties.Value)
            {
                newMetadata.Value = metadata.Value;
            }

            return newMetadata;
        }

        public static IEnumerable<IEntityValue> CreateMetadataValues(IEntityValue parent, ValueEditor editor, IUIEditorMetadataPropertiesAvailableStatus requestedProperties)
        {
            foreach (NameValuePair metadataPair in editor.Metadata)
            {
                IEntityValue metadataValue = CreateMetadataValue(parent.EntityRuntime, metadataPair, requestedProperties);
                yield return metadataValue;
            }
        }
    }
}
