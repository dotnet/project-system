// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class ILaunchSettingsProviderFactory
    {
        /// <summary>
        /// Creates a mock <see cref="ILaunchSettingsProvider"/> for testing purposes.
        /// </summary>
        /// <param name="activeProfileName">The name of the active profile in the new object.</param>
        /// <param name="launchProfiles">The set of launch profiles to expose through the provider.</param>
        /// <param name="setActiveProfileCallback">An optional method to call when the active profile is set.</param>
        /// <param name="updateLaunchSettingsCallback">An optional method to call when the when the set of launch settings is updated</param>
        public static ILaunchSettingsProvider Create(
            string? activeProfileName = null,
            IEnumerable<ILaunchProfile>? launchProfiles = null,
            Action<string>? setActiveProfileCallback = null,
            Action<ILaunchSettings>? updateLaunchSettingsCallback = null)
        {
            var launchSettingsMock = new Mock<ILaunchSettings>();

            if (launchProfiles != null)
            {
                launchSettingsMock.Setup(t => t.Profiles).Returns(launchProfiles.ToImmutableList());
            }

            if (activeProfileName != null)
            {
                var activeLaunchProfile = launchProfiles?.FirstOrDefault(p => p.Name == activeProfileName)
                    ?? new LaunchProfile { Name = activeProfileName };
                launchSettingsMock.Setup(t => t.ActiveProfile).Returns(activeLaunchProfile);
            }

            var launchSettings = launchSettingsMock.Object;

            var settingsProviderMock = new Mock<ILaunchSettingsProvider>();
            settingsProviderMock.Setup(t => t.WaitForFirstSnapshot(It.IsAny<int>())).Returns(Task.FromResult<ILaunchSettings?>(launchSettings));

            if (setActiveProfileCallback != null)
            {
                settingsProviderMock.Setup(t => t.SetActiveProfileAsync(It.IsAny<string>()))
                    .Returns<string>(v =>
                    {
                        setActiveProfileCallback(v);
                        return Task.CompletedTask;
                    });
            }

            if (updateLaunchSettingsCallback != null)
            {
                settingsProviderMock.Setup(t => t.UpdateAndSaveSettingsAsync(It.IsAny<ILaunchSettings>()))
                    .Returns<ILaunchSettings>(v =>
                    {
                        updateLaunchSettingsCallback(v);
                        return Task.CompletedTask;
                    });
            }

            var settingsProvider = settingsProviderMock.Object;
            return settingsProvider;
        }
    }
}
