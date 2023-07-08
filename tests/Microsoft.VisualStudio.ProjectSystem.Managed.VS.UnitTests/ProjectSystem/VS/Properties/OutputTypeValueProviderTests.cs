// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public class OutputTypeValueProviderTests
    {
        [Theory]
        [InlineData("WinExe", "0")]
        [InlineData("Exe", "1")]
        [InlineData("exe", "1")]
        [InlineData("Library", "2")]
        [InlineData("WinMDObj", "2")]
        [InlineData("AppContainerExe", "1")]
        [InlineData("", "0")]
        public async Task GetEvaluatedValue(object outputTypePropertyValue, string expectedMappedValue)
        {
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputTypeProperty, outputTypePropertyValue));
            var provider = new OutputTypeValueProvider(properties);

            var actualPropertyValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, null!);
            Assert.Equal(expectedMappedValue, actualPropertyValue);
        }

        [Theory]
        [InlineData("0", "WinExe")]
        [InlineData("1", "Exe")]
        [InlineData("2", "Library")]
        public async Task SetValue(string incomingValue, string expectedOutputTypeValue)
        {
            var setValues = new List<object>();
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputTypeProperty, "InitialValue", setValues));
            var provider = new OutputTypeValueProvider(properties);

            await provider.OnSetPropertyValueAsync(string.Empty, incomingValue, null!);
            Assert.Equal(setValues.Single(), expectedOutputTypeValue);
        }

        [Theory]
        [InlineData("3")]
        [InlineData("InvalidValue")]
        public async Task SetValue_ThrowsKeyNotFoundException(string invalidValue)
        {
            var setValues = new List<object>();
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputTypeProperty, "InitialValue", setValues));
            var provider = new OutputTypeValueProvider(properties);

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await provider.OnSetPropertyValueAsync(string.Empty, invalidValue, null!);
            });
        }
    }
}
