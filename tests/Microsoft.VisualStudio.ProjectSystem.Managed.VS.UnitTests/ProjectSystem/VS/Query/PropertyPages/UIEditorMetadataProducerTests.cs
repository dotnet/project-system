// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Xunit;

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
            var producer = new TestUIEditorMetadataProducer(properties);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var metadata = new NameValuePair { Name = "Alpha", Value = "AlphaValue" };

            var result = (UIEditorMetadataValue)producer.TestCreateMetadataValueAsync(entityRuntime, metadata);

            Assert.Equal(expected: "Alpha", actual: result.Name);
            Assert.Equal(expected: "AlphaValue", actual: result.Value);
        }

        [Fact]
        public void WhenPropertiesAreNotRequested_PropertyValuesAreNotReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIEditorMetadataAvailableStatus(
                includeName: false,
                includeValue: false);
            var producer = new TestUIEditorMetadataProducer(properties);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var metadata = new NameValuePair { Name = "Alpha", Value = "AlphaValue" };

            var result = (UIEditorMetadataValue)producer.TestCreateMetadataValueAsync(entityRuntime, metadata);

            Assert.Throws<MissingDataException>(() => result.Name);
            Assert.Throws<MissingDataException>(() => result.Value);
        }

        /// <summary>
        /// Derives from the abstract <see cref="UIEditorMetadataProducer"/>. Also exposes protected members for testing
        /// purposes.
        /// </summary>
        private class TestUIEditorMetadataProducer : UIEditorMetadataProducer
        {
            public TestUIEditorMetadataProducer(IUIEditorMetadataPropertiesAvailableStatus properties)
                : base(properties)
            {
            }

            public IEntityValue TestCreateMetadataValueAsync(IEntityRuntimeModel entityRuntime, NameValuePair metadata)
            {
                return CreateMetadataValue(entityRuntime, metadata);
            }
        }
    }
}
