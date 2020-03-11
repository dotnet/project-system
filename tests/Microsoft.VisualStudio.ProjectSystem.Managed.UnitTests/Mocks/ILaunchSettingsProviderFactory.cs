// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        /// <param name="setActiveProfileCallback">An optional method to call when the active profile is set.</param>
        public static ILaunchSettingsProvider Create(
            string activeProfileName,
            Action<string>? setActiveProfileCallback = null)
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
