// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class OutputTypeValueProviderTests
    {
        [Theory]
        [InlineData("WinExe", "0")]
        [InlineData("Exe", "1")]
        [InlineData("Library", "2")]
        [InlineData("WinMDObj", "2")]
        [InlineData("AppContainerExe", "1")]
        public async void GetEvaluatedValue(object outputTypePropertyValue, string expectedMappedValue)
        {
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData()
                {
                    Category = ConfigurationGeneral.SchemaName,
                    PropertyName = ConfigurationGeneral.OutputTypeProperty,
                    Value = outputTypePropertyValue
                });
            var provider = new OutputTypeValueProvider(properties);

            var actualPropertyValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, null);
            Assert.Equal(expectedMappedValue, actualPropertyValue);
        }

        [Fact]
        public async void GetEvaluatedValue_ThrowsKeyNotFoundException()
        {
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData()
                {
                    Category = ConfigurationGeneral.SchemaName,
                    PropertyName = ConfigurationGeneral.OutputTypeProperty,
                    Value = "InvalidValue"
                });
            var provider = new OutputTypeValueProvider(properties);

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, null);
            });
        }

        [Theory]
        [InlineData("0", "WinExe")]
        [InlineData("1", "Exe")]
        [InlineData("2", "Library")]
        public async void SetValue(string incomingValue, string expectedOutputTypeValue)
        {
            var setValues = new List<object>();
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData()
                {
                    Category = ConfigurationGeneral.SchemaName,
                    PropertyName = ConfigurationGeneral.OutputTypeProperty,
                    Value = "InitialValue",
                    SetValues = setValues
                });
            var provider = new OutputTypeValueProvider(properties);

            var actualPropertyValue = await provider.OnSetPropertyValueAsync(incomingValue, null);
            Assert.Equal(setValues.Single(), expectedOutputTypeValue);
        }

        [Theory]
        [InlineData("3")]
        [InlineData("InvalidValue")]
        public async void SetValue_ThrowsKeyNotFoundException(string invalidValue)
        {
            var setValues = new List<object>();
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData()
                {
                    Category = ConfigurationGeneral.SchemaName,
                    PropertyName = ConfigurationGeneral.OutputTypeProperty,
                    Value = "InitialValue",
                    SetValues = setValues
                });
            var provider = new OutputTypeValueProvider(properties);

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await provider.OnSetPropertyValueAsync(invalidValue, null);
            });
        }
    }
}
