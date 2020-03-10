// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class ActiveLaunchProfilesValueProviderTests
    {
        [Fact]
        public async Task OnGetEvaluatedProperty_GetsActiveProfileName()
        {
            string activeProfileName = "Alpha";
            var settingsProvider = CreateLaunchSettingsProvider(activeProfileName);

            var launchProfileProvider = new ActiveLaunchProfileValueProvider(settingsProvider);

            var actualValue = await launchProfileProvider.OnGetEvaluatedPropertyValueAsync(string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileName, actual: actualValue);
        }

        [Fact]
        public async Task OnGetUnevaluatedProperty_GetsActiveProfileName()
        {
            string activeProfileName = "Beta";
            var settingsProvider = CreateLaunchSettingsProvider(activeProfileName);
            var launchProfileProvider = new ActiveLaunchProfileValueProvider(settingsProvider);

            var actualValue = await launchProfileProvider.OnGetUnevaluatedPropertyValueAsync(string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileName, actual: actualValue);
        }

        [Fact]
        public async Task OnSetPropertyValue_SetsActiveProfile()
        {
            string activeProfileName = "Gamma";
            var settingsProvider = CreateLaunchSettingsProvider(activeProfileName, v => activeProfileName = v);
            var launchProfileProvider = new ActiveLaunchProfileValueProvider(settingsProvider);

            var result = await launchProfileProvider.OnSetPropertyValueAsync("Delta", Mock.Of<IProjectProperties>());

            Assert.Null(result);
            Assert.Equal(expected: "Delta", actual: activeProfileName);
        }

        /// <summary>
        /// Creates a mock <see cref="ILaunchSettingsProvider"/> for testing purposes.
        /// </summary>
        /// <param name="activeProfileName">The name of the active profile in the new object.</param>
        /// <param name="setActiveProfileCallback">An optional method to call when the active profile is set.</param>
        private static ILaunchSettingsProvider CreateLaunchSettingsProvider(string activeProfileName, Action<string>? setActiveProfileCallback = null)
        {
            var launchProfile = new LaunchProfile { Name = activeProfileName };

            var launchSettingsMock = new Mock<ILaunchSettings>();
            launchSettingsMock.Setup(t => t.ActiveProfile).Returns(launchProfile);
            var launchSettings = launchSettingsMock.Object;

            var settingsProviderMock = new Mock<ILaunchSettingsProvider>();
            settingsProviderMock.Setup(t => t.WaitForFirstSnapshot(It.IsAny<int>())).Returns(Task.FromResult(launchSettings));

            if (setActiveProfileCallback != null)
            {
                settingsProviderMock.Setup(t => t.SetActiveProfileAsync(It.IsAny<string>()))
                    .Returns<string>(v =>
                    {
                        setActiveProfileCallback(v);
                        return Task.CompletedTask;
                    });
            }

            var settingsProvider = settingsProviderMock.Object;
            return settingsProvider;
        }
    }
}
