// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public class OutputTypeExValueProviderTests
    {
        [Theory]
        [InlineData("WinExe", "0")]
        [InlineData("Exe", "1")]
        [InlineData("exe", "1")]
        [InlineData("Library", "2")]
        [InlineData("WinMDObj", "3")]
        [InlineData("AppContainerExe", "4")]
        [InlineData("", "0")]
        public async Task GetEvaluatedValue(object outputTypePropertyValue, string expectedMappedValue)
        {
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputTypeProperty, outputTypePropertyValue));
            var provider = new OutputTypeExValueProvider(properties);

            var actualPropertyValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, null!);
            Assert.Equal(expectedMappedValue, actualPropertyValue);
        }

        [Theory]
        [InlineData("0", "WinExe")]
        [InlineData("1", "Exe")]
        [InlineData("2", "Library")]
        [InlineData("3", "WinMDObj")]
        [InlineData("4", "AppContainerExe")]
        public async Task SetValue(string incomingValue, string expectedOutputTypeValue)
        {
            var setValues = new List<object>();
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputTypeProperty, "InitialValue", setValues));
            var provider = new OutputTypeExValueProvider(properties);
            await provider.OnSetPropertyValueAsync(string.Empty, incomingValue, null!);

            Assert.Equal(setValues.Single(), expectedOutputTypeValue);
        }

        [Fact]
        public async Task SetValue_ThrowsKeyNotFoundException()
        {
            var setValues = new List<object>();
            var properties = ProjectPropertiesFactory.Create(
                UnconfiguredProjectFactory.Create(),
                new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputTypeProperty, "InitialValue", setValues));
            var provider = new OutputTypeExValueProvider(properties);

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await provider.OnSetPropertyValueAsync(string.Empty, "InvalidValue", null!);
            });
        }
    }
}
