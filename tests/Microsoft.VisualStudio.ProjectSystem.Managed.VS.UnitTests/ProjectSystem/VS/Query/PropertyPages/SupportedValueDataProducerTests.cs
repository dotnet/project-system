// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class SupportedValueDataProducerTests
    {
        [Fact]
        public void WhenPropertiesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateSupportedValuesPropertiesAvailableStatus(includeAllProperties: true);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var enumValue = IEnumValueFactory.Create(displayName: "Hello", name: "MyValue");

            var result = (SupportedValueSnapshot)SupportedValueDataProducer.CreateSupportedValue(entityRuntime, enumValue, properties);

            Assert.Equal(expected: "Hello", actual: result.DisplayName);
            Assert.Equal(expected: "MyValue", actual: result.Value);
        }

        [Fact]
        public async Task WhenCreatingValuesFromAnIProperty_WeGetOneValuePerIEnumValue()
        {
            var properties = PropertiesAvailableStatusFactory.CreateSupportedValuesPropertiesAvailableStatus(includeAllProperties: true);

            var parentEntity = IEntityWithIdFactory.Create("ParentKey", "ParentKeyValue");
            var iproperty = IPropertyFactory.CreateEnum(new[]
            {
                IEnumValueFactory.Create(displayName: "Alpha", name: "a"),
                IEnumValueFactory.Create(displayName: "Beta", name: "b"),
                IEnumValueFactory.Create(displayName: "Gamma", name: "c")
            });

            var result = await SupportedValueDataProducer.CreateSupportedValuesAsync(parentEntity, iproperty, properties);

            Assert.Collection(result, new Action<IEntityValue>[]
            {
                entity => assertEqual(entity, expectedDisplayName: "Alpha", expectedValue: "a"),
                entity => assertEqual(entity, expectedDisplayName: "Beta", expectedValue: "b"),
                entity => assertEqual(entity, expectedDisplayName: "Gamma", expectedValue: "c")
            });

            static void assertEqual(IEntityValue entity, string expectedDisplayName, string expectedValue)
            {
                var supportedValueEntity = (SupportedValueSnapshot)entity;
                Assert.Equal(expectedDisplayName, supportedValueEntity.DisplayName);
                Assert.Equal(expectedValue, supportedValueEntity.Value);
            }
        }
    }
}
