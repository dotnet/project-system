// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class ExecutablePathValueProviderTests
    {
        [Fact]
        public async Task ExecutablePath_OnGetEvaluatedPropertyValueAsync_GetsExecutableFromActiveProfile()
        {
            string activeProfileName = "Alpha";
            string activeProfileLaunchTarget = "AlphaCommand";
            string activeProfileExecutablePath = @"C:\user\bin\alpha.exe";
            var profile = new WritableLaunchProfile
            {
                Name = activeProfileName,
                CommandName = activeProfileLaunchTarget,
                ExecutablePath = activeProfileExecutablePath
            };
            var settingsProvider = ILaunchSettingsProviderFactory.Create(activeProfileName, new[] { profile.ToLaunchProfile() });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new ExecutablePathValueProvider(project, settingsProvider, threadingService);

            var actualValue = await launchProfileProvider.OnGetEvaluatedPropertyValueAsync(string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileExecutablePath, actual: actualValue);
        }

        [Fact]
        public async Task ExecutablePath_OnGetUnevaluatedPropertyValueAsync_GetsExecutableFromActiveProfile()
        {
            string activeProfileName = "Beta";
            string activeProfileLaunchTarget = "BetaCommand";
            string activeProfileExecutablePath = @"C:\user\bin\beta.exe";
            var profile = new WritableLaunchProfile
            {
                Name = activeProfileName,
                CommandName = activeProfileLaunchTarget,
                ExecutablePath = activeProfileExecutablePath
            };
            var settingsProvider = ILaunchSettingsProviderFactory.Create(activeProfileName, new[] { profile.ToLaunchProfile() });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new ExecutablePathValueProvider(project, settingsProvider, threadingService);

            var actualValue = await launchProfileProvider.OnGetUnevaluatedPropertyValueAsync(string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileExecutablePath, actual: actualValue);
        }

        [Fact]
        public async Task ExecutablePath_OnSetPropertyValueAsync_SetsTargetInActiveProfile()
        {
            string activeProfileName = "Gamma";
            string activeProfileLaunchTarget = "GammaCommand";
            string activeProfileExecutablePath = @"C:\user\bin\gamma.exe";
            var profile = new WritableLaunchProfile
            {
                Name = activeProfileName,
                CommandName = activeProfileLaunchTarget,
                ExecutablePath = activeProfileExecutablePath
            };
            var settingsProvider = ILaunchSettingsProviderFactory.Create(
                activeProfileName,
                new[] { profile.ToLaunchProfile() },
                updateLaunchSettingsCallback: s =>
                {
                    activeProfileExecutablePath = s.ActiveProfile!.ExecutablePath;
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new ExecutablePathValueProvider(project, settingsProvider, threadingService);

            await launchProfileProvider.OnSetPropertyValueAsync(@"C:\user\bin\delta.exe", Mock.Of<IProjectProperties>());

            Assert.Equal(expected: @"C:\user\bin\delta.exe", actual: activeProfileExecutablePath);
        }
    }
}
