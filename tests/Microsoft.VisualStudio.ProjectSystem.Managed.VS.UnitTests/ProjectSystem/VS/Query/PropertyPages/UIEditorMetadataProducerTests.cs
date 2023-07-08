// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class UIEditorMetadataProducerTests
    {
        [Fact]
        public void WhenPropertiesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIEditorMetadataAvailableStatus(
                includeName: true,
                includeValue: true);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var metadata = new NameValuePair { Name = "Alpha", Value = "AlphaValue" };
           
            var result = (UIEditorMetadataSnapshot)UIEditorMetadataProducer.CreateMetadataValue(entityRuntime, metadata, properties);

            Assert.Equal(expected: "Alpha", actual: result.Name);
            Assert.Equal(expected: "AlphaValue", actual: result.Value);
        }

        [Fact]
        public void WhenPropertiesAreNotRequested_PropertyValuesAreNotReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIEditorMetadataAvailableStatus(includeAllProperties: false);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var metadata = new NameValuePair { Name = "Alpha", Value = "AlphaValue" };

            var result = (UIEditorMetadataSnapshot)UIEditorMetadataProducer.CreateMetadataValue(entityRuntime, metadata, properties);

            Assert.Throws<MissingDataException>(() => result.Name);
            Assert.Throws<MissingDataException>(() => result.Value);
        }

        [Fact]
        public void WhenCreatingMetadataFromAnEditor_AllMetadataIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIEditorMetadataAvailableStatus(
                includeName: true,
                includeValue: true);

            var parentEntity = IEntityWithIdFactory.Create("ParentKey", "ParentKeyValue");
            var editor = new ValueEditor
            {
                Metadata =
                { 
                    new() { Name = "Alpha", Value = "A" },
                    new() { Name = "Beta", Value = "B" }
                }
            };

            var results = UIEditorMetadataProducer.CreateMetadataValues(
                parentEntity,
                editor,
                properties);

            Assert.Contains(results, entity => entity is UIEditorMetadataSnapshot { Name: "Alpha", Value: "A" });
            Assert.Contains(results, entity => entity is UIEditorMetadataSnapshot { Name: "Beta",  Value: "B" });
        }
    }
}
