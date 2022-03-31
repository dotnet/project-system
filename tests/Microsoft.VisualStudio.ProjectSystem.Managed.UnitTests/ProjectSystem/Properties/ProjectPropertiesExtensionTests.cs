// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class ProjectPropertiesExtensionTests
    {
        [Fact]
        public async Task WhenThePropertyIsSetInTheProject_TheValueIsSavedInTemporaryStorage()
        {
            IProjectProperties projectProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(
                propertyNameAndValues: new Dictionary<string, string?>()
                {
                    { "MyProperty", "Alpha" }
                },
                inheritedPropertyNames: new());

            Dictionary<string, string> storedValues = new();
            ITemporaryPropertyStorage temporaryPropertyStorage = ITemporaryPropertyStorageFactory.Create(values: storedValues);

            await projectProperties.SaveValueIfCurrentlySetAsync("MyProperty", temporaryPropertyStorage);

            Assert.Contains(new KeyValuePair<string, string>("MyProperty", "Alpha"), storedValues);
        }

        [Fact]
        public async Task WhenThePropertyIsInherited_TheValueIsNotSavedInTemporaryStorage()
        {
            IProjectProperties projectProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(
                propertyNameAndValues: new Dictionary<string, string?>()
                {
                    { "MyProperty", "Alpha" }
                },
                inheritedPropertyNames: new() { "MyProperty" });

            Dictionary<string, string> storedValues = new();
            ITemporaryPropertyStorage temporaryPropertyStorage = ITemporaryPropertyStorageFactory.Create(values: storedValues);

            await projectProperties.SaveValueIfCurrentlySetAsync("MyProperty", temporaryPropertyStorage);

            Assert.DoesNotContain(new KeyValuePair<string, string>("MyProperty", "Alpha"), storedValues);
        }

        [Fact]
        public async Task WhenThePropertyIsSetInTheProject_TheSavedValueIsNotRestored()
        {
            Dictionary<string, string?> propertyNamesAndValues = new()
            {
                { "MyProperty", "Beta" }
            };
            IProjectProperties projectProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(
                propertyNamesAndValues,
                inheritedPropertyNames: new());

            ITemporaryPropertyStorage temporaryPropertyStorage = ITemporaryPropertyStorageFactory.Create(
                values: new() { { "MyProperty", "Alpha" } });

            await projectProperties.RestoreValueIfNotCurrentlySetAsync("MyProperty", temporaryPropertyStorage);

            Assert.Contains(new KeyValuePair<string, string?>("MyProperty", "Beta"), propertyNamesAndValues);
        }

        [Fact]
        public async Task WhenThePropertyIsInherited_TheSavedValueIsRestored()
        {
            Dictionary<string, string?> propertyNamesAndValues = new()
            {
                { "MyProperty", "Beta" }
            };
            IProjectProperties projectProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(
                propertyNamesAndValues,
                inheritedPropertyNames: new() { "MyProperty" });

            ITemporaryPropertyStorage temporaryPropertyStorage = ITemporaryPropertyStorageFactory.Create(
                values: new() { { "MyProperty", "Alpha" } });

            await projectProperties.RestoreValueIfNotCurrentlySetAsync("MyProperty", temporaryPropertyStorage);

            Assert.Contains(new KeyValuePair<string, string?>("MyProperty", "Alpha"), propertyNamesAndValues);
        }

        [Fact]
        public async Task WhenThePropertyIsNotSetAtAll_TheSavedValueIsRestored()
        {
            Dictionary<string, string?> propertyNamesAndValues = new()
            {
                { "MyProperty", null }
            };
            IProjectProperties projectProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(
                propertyNamesAndValues,
                inheritedPropertyNames: new() { "MyProperty" }); // If a property isn't defined _at all_ it is considered inherited.

            ITemporaryPropertyStorage temporaryPropertyStorage = ITemporaryPropertyStorageFactory.Create(
                values: new() { { "MyProperty", "Alpha" } });

            await projectProperties.RestoreValueIfNotCurrentlySetAsync("MyProperty", temporaryPropertyStorage);

            Assert.Contains(new KeyValuePair<string, string?>("MyProperty", "Alpha"), propertyNamesAndValues);
        }
    }
}
