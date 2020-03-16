// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class ActiveLaunchProfilesValueProviderTests
    {
        [Fact]
        public async Task ActiveLaunchProfileName_OnGetEvaluatedProperty_GetsActiveProfileName()
        {
            string activeProfileName = "Alpha";
            var settingsProvider = ILaunchSettingsProviderFactory.Create(activeProfileName);

            var launchProfileProvider = new ActiveLaunchProfileNameValueProvider(settingsProvider);

            var actualValue = await launchProfileProvider.OnGetEvaluatedPropertyValueAsync(string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileName, actual: actualValue);
        }

        [Fact]
        public async Task ActiveLaunchProfileName_OnGetUnevaluatedProperty_GetsActiveProfileName()
        {
            string activeProfileName = "Beta";
            var settingsProvider = ILaunchSettingsProviderFactory.Create(activeProfileName);
            var launchProfileProvider = new ActiveLaunchProfileNameValueProvider(settingsProvider);

            var actualValue = await launchProfileProvider.OnGetUnevaluatedPropertyValueAsync(string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileName, actual: actualValue);
        }

        [Fact]
        public async Task ActiveLaunchProfileName_OnSetPropertyValue_SetsActiveProfile()
        {
            string activeProfileName = "Gamma";
            var settingsProvider = ILaunchSettingsProviderFactory.Create(activeProfileName, setActiveProfileCallback: v => activeProfileName = v);
            var launchProfileProvider = new ActiveLaunchProfileNameValueProvider(settingsProvider);

            var result = await launchProfileProvider.OnSetPropertyValueAsync("Delta", Mock.Of<IProjectProperties>());

            Assert.Null(result);
            Assert.Equal(expected: "Delta", actual: activeProfileName);
        }
    }
}
