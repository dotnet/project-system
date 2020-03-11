// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.InterceptedProjectProperties
{
    public class LaunchTargetValueProviderTests
    {
        [Fact]
        public async Task OnGetEvaluatedPropertyValueAsync_GetsTargetFromActiveProfile()
        {
            string activeProfileName = "Alpha";
            string activeProfileLaunchTarget = "AlphaCommand";
            var profile = new WritableLaunchProfile
            {
                Name = activeProfileName,
                CommandName = activeProfileLaunchTarget
            };
            var settingsProvider = ILaunchSettingsProviderFactory.Create(activeProfileName, new[] { profile.ToLaunchProfile() });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new LaunchTargetValueProvider(project, settingsProvider, threadingService);

            var actualValue = await launchProfileProvider.OnGetEvaluatedPropertyValueAsync(string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileLaunchTarget, actual: actualValue);

        }

        [Fact]
        public async Task OnGetUnevaluatedPropertyValueAsync_GetsTargetFromActiveProfile()
        {
            string activeProfileName = "Beta";
            string activeProfileLaunchTarget = "BetaCommand";
            var profile = new WritableLaunchProfile
            {
                Name = activeProfileName,
                CommandName = activeProfileLaunchTarget
            };
            var settingsProvider = ILaunchSettingsProviderFactory.Create(activeProfileName, new[] { profile.ToLaunchProfile() });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new LaunchTargetValueProvider(project, settingsProvider, threadingService);

            var actualValue = await launchProfileProvider.OnGetEvaluatedPropertyValueAsync(string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileLaunchTarget, actual: actualValue);
        }

        [Fact]
        public async Task OnSetPropertyValueAsync_SetsTargetInActiveProfile()
        {
            string activeProfileName = "Gamma";
            string activeProfileLaunchTarget = "GammaCommand";
            var profile = new WritableLaunchProfile
            {
                Name = activeProfileName,
                CommandName = activeProfileLaunchTarget
            };
            var settingsProvider = ILaunchSettingsProviderFactory.Create(
                activeProfileName,
                new[] { profile.ToLaunchProfile() },
                updateLaunchSettingsCallback: s =>
                {
                    activeProfileLaunchTarget = s.ActiveProfile!.CommandName;
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new LaunchTargetValueProvider(project, settingsProvider, threadingService);

            await launchProfileProvider.OnSetPropertyValueAsync("NewCommand", Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "NewCommand", actual: activeProfileLaunchTarget);
        }
    }
}
