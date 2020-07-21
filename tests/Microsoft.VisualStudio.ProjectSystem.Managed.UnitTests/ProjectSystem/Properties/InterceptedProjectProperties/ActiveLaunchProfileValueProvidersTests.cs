// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class ActiveLaunchProfileValueProvidersTests
    {
        [Fact]
        public async Task ActiveLaunchProfileName_OnGetEvaluatedProperty_GetsActiveProfileName()
        {
            string activeProfileName = "Alpha";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName);

            var launchProfileProvider = new ActiveLaunchProfileNameValueProvider(settingsProvider);

            var actualValue = await launchProfileProvider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileName, actual: actualValue);
        }

        [Fact]
        public async Task ActiveLaunchProfileName_OnGetUnevaluatedProperty_GetsActiveProfileName()
        {
            string activeProfileName = "Beta";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName);
            var launchProfileProvider = new ActiveLaunchProfileNameValueProvider(settingsProvider);

            var actualValue = await launchProfileProvider.OnGetUnevaluatedPropertyValueAsync(string.Empty, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileName, actual: actualValue);
        }

        [Fact]
        public async Task ActiveLaunchProfileName_OnSetPropertyValue_SetsActiveProfile()
        {
            string activeProfileName = "Gamma";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName, setActiveProfileCallback: v => activeProfileName = v);
            var launchProfileProvider = new ActiveLaunchProfileNameValueProvider(settingsProvider);

            var result = await launchProfileProvider.OnSetPropertyValueAsync(string.Empty, "Delta", Mock.Of<IProjectProperties>());

            Assert.Null(result);
            Assert.Equal(expected: "Delta", actual: activeProfileName);
        }

        [Fact]
        public async Task ExecutablePath_OnGetEvaluatedPropertyValueAsync_GetsExecutableFromActiveProfile()
        {
            string activeProfileExecutablePath = @"C:\user\bin\alpha.exe";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "Alpha", activeProfileExecutablePath: activeProfileExecutablePath);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            var actualValue = await launchProfileProvider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.ExecutablePathPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileExecutablePath, actual: actualValue);
        }

        [Fact]
        public async Task ExecutablePath_OnGetUnevaluatedPropertyValueAsync_GetsExecutableFromActiveProfile()
        {
            string activeProfileExecutablePath = @"C:\user\bin\beta.exe";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "Beta", activeProfileExecutablePath: activeProfileExecutablePath);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            var actualValue = await launchProfileProvider.OnGetUnevaluatedPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.ExecutablePathPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileExecutablePath, actual: actualValue);
        }

        [Fact]
        public async Task ExecutablePath_OnSetPropertyValueAsync_SetsTargetInActiveProfile()
        {
            string? activeProfileExecutablePath = @"C:\user\bin\gamma.exe";
            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "Gamma",
                activeProfileExecutablePath: activeProfileExecutablePath,
                updateLaunchSettingsCallback: s =>
                {
                    activeProfileExecutablePath = s.ActiveProfile!.ExecutablePath;
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            await launchProfileProvider.OnSetPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.ExecutablePathPropertyName, @"C:\user\bin\delta.exe", Mock.Of<IProjectProperties>());

            Assert.Equal(expected: @"C:\user\bin\delta.exe", actual: activeProfileExecutablePath);
        }

        [Fact]
        public async Task LaunchTarget_OnGetEvaluatedPropertyValueAsync_GetsTargetFromActiveProfile()
        {
            string activeProfileLaunchTarget = "AlphaCommand";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "Alpha", activeProfileLaunchTarget: activeProfileLaunchTarget);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            var actualValue = await launchProfileProvider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.LaunchTargetPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileLaunchTarget, actual: actualValue);
        }

        [Fact]
        public async Task LaunchTarget_OnGetUnevaluatedPropertyValueAsync_GetsTargetFromActiveProfile()
        {
            string activeProfileLaunchTarget = "BetaCommand";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "Beta", activeProfileLaunchTarget: activeProfileLaunchTarget);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            var actualValue = await launchProfileProvider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.LaunchTargetPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileLaunchTarget, actual: actualValue);
        }

        [Fact]
        public async Task LaunchTarget_OnSetPropertyValueAsync_SetsTargetInActiveProfile()
        {
            string? activeProfileLaunchTarget = "GammaCommand";
            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "Gamma",
                activeProfileLaunchTarget,
                updateLaunchSettingsCallback: s =>
                {
                    activeProfileLaunchTarget = s.ActiveProfile!.CommandName;
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var launchProfileProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            await launchProfileProvider.OnSetPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.LaunchTargetPropertyName, "NewCommand", Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "NewCommand", actual: activeProfileLaunchTarget);
        }

        [Fact]
        public async Task CommandLineArguments_OnGetEvaluatedPropertyValueAsync_GetsArgumentsFromActiveProfile()
        {
            string activeProfileCommandLineArguments = "/bird:YES /giraffe:NO";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "ZooAnimals", activeProfileCommandLineArgs: activeProfileCommandLineArguments);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var commandLineArgumentsProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            var actualValue = await commandLineArgumentsProvider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.CommandLineArgumentsPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileCommandLineArguments, actual: actualValue);
        }

        [Fact]
        public async Task CommandLineArguments_OnGetUnevaluatedPropertyValueAsync_GetsArgumentsFromActiveProfile()
        {
            string activeProfileCommandLineArguments = "/alpaca:YES /llama:NO /vicuña:NONONO";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "SortOfFarmAnimals", activeProfileCommandLineArgs: activeProfileCommandLineArguments);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var commandLineArgumentsProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            var actualValue = await commandLineArgumentsProvider.OnGetUnevaluatedPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.CommandLineArgumentsPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileCommandLineArguments, actual: actualValue);
        }

        [Fact]
        public async Task CommandLineArguments_OnSetPropertyValueAsync_SetsArgumentsInActiveProfile()
        {
            string? activeProfileCommandLineArgs = "/orca:YES /bluewhale:NO";
            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "SeaMammals",
                activeProfileCommandLineArgs: activeProfileCommandLineArgs,
                updateLaunchSettingsCallback: s =>
                {
                    activeProfileCommandLineArgs = s.ActiveProfile!.CommandLineArgs;
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var commandLineArgumentsProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            await commandLineArgumentsProvider.OnSetPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.CommandLineArgumentsPropertyName, "/seaotters:YES /seals:YES", Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "/seaotters:YES /seals:YES", actual: activeProfileCommandLineArgs);
        }

        [Fact]
        public async Task WorkingDirectory_OnGetEvaluatedPropertyValueAsync_GetsDirectoryFromActiveProfile()
        {
            string activeProfileWorkingDirectory = @"C:\alpha\beta\gamma";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "One", activeProfileWorkingDirectory: activeProfileWorkingDirectory);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var workingDirectoryProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            var actualValue = await workingDirectoryProvider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.WorkingDirectoryPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileWorkingDirectory, actual: actualValue);
        }

        [Fact]
        public async Task WorkingDirectory_OnGetUnevaluatedPropertyValueAsync_GetsDirectoryFromActiveProfile()
        {
            string activeProfileWorkingDirectory = @"C:\delta\epsilon\phi";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "Two", activeProfileWorkingDirectory: activeProfileWorkingDirectory);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var workingDirectoryProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            var actualValue = await workingDirectoryProvider.OnGetUnevaluatedPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.WorkingDirectoryPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileWorkingDirectory, actual: actualValue);
        }

        [Fact]
        public async Task WorkingDirectory_OnSetPropertyValueAsync_SetsDirectoryInActiveProfile()
        {
            string? activeProfileWorkingDirectory = @"C:\one\two\three";
            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "Three",
                activeProfileWorkingDirectory: activeProfileWorkingDirectory,
                updateLaunchSettingsCallback: s =>
                {
                    activeProfileWorkingDirectory = s.ActiveProfile!.WorkingDirectory;
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var workingDirectoryProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            await workingDirectoryProvider.OnSetPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.WorkingDirectoryPropertyName, @"C:\four\five\six", Mock.Of<IProjectProperties>());

            Assert.Equal(expected: @"C:\four\five\six", actual: activeProfileWorkingDirectory);
        }

        [Fact]
        public async Task LaunchBrowser_OnGetEvaluatedPropertyValueAsync_GetsValueFromActiveProfile()
        {
            bool activeProfileLaunchBrowser = true;
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "One", activeProfileLaunchBrowser: activeProfileLaunchBrowser);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var workingDirectoryProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            var actualValue = await workingDirectoryProvider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.LaunchBrowserPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public async Task LaunchBrowser_OnSetPropertyValueAsync_SetsValueInActiveProfile()
        {
            bool activeProfileLaunchBrowser = false;
            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "Three",
                activeProfileLaunchBrowser: activeProfileLaunchBrowser,
                updateLaunchSettingsCallback: s =>
                {
                    activeProfileLaunchBrowser = s.ActiveProfile!.LaunchBrowser;
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var workingDirectoryProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            await workingDirectoryProvider.OnSetPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.LaunchBrowserPropertyName, "true", Mock.Of<IProjectProperties>());

            Assert.True(activeProfileLaunchBrowser);
        }

        [Fact]
        public async Task LaunchUrl_OnGetEvaluatedPropertyValueAsync_GetsUrlFromActiveProfile()
        {
            string activeProfileLaunchUrl = "https://microsoft.com";
            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "One", activeProfileLaunchUrl: activeProfileLaunchUrl);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var workingDirectoryProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            var actualValue = await workingDirectoryProvider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.LaunchUrlPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileLaunchUrl, actual: actualValue);
        }

        [Fact]
        public async Task LaunchUrl_OnSetPropertyValueAsync_SetsUrlInActiveProfile()
        {
            string? activeProfileLaunchUrl = "https://incorrect.com";
            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "Three",
                activeProfileLaunchUrl: activeProfileLaunchUrl,
                updateLaunchSettingsCallback: s =>
                {
                    activeProfileLaunchUrl = s.ActiveProfile!.LaunchUrl;
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var workingDirectoryProvider = new ActiveLaunchProfileCommonValueProvider(project, settingsProvider, threadingService);

            await workingDirectoryProvider.OnSetPropertyValueAsync(ActiveLaunchProfileCommonValueProvider.LaunchUrlPropertyName, "https://microsoft.com", Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "https://microsoft.com", actual: activeProfileLaunchUrl);
        }
        [Fact]
        public async Task AuthenticationMode_OnGetEvaluatedPropertyValueAsync_GetsModeFromActiveProfile()
        {
            string activeProfileAuthenticationMode = "Windows";
            var activeProfileOtherSettings = new Dictionary<string, object>
            {
                { LaunchProfileExtensions.RemoteAuthenticationModeProperty, activeProfileAuthenticationMode }
            };

            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "One", activeProfileOtherSettings: activeProfileOtherSettings);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var provider = new ActiveLaunchProfileExtensionValueProvider(project, settingsProvider, threadingService);

            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileExtensionValueProvider.AuthenticationModePropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileAuthenticationMode, actual: actualValue);
        }

        [Fact]
        public async Task AuthenticationMode_OnSetPropertyValueAsync_SetsDirectoryInActiveProfile()
        {
            string? activeProfileAuthenticationMode = "Windows";
            var activeProfileOtherSettings = new Dictionary<string, object>
            {
                { LaunchProfileExtensions.RemoteAuthenticationModeProperty, activeProfileAuthenticationMode }
            };

            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "One",
                activeProfileOtherSettings: activeProfileOtherSettings,
                updateLaunchSettingsCallback: s =>
                {
                    Assumes.NotNull(s.ActiveProfile?.OtherSettings);
                    activeProfileAuthenticationMode = (string)s.ActiveProfile.OtherSettings[LaunchProfileExtensions.RemoteAuthenticationModeProperty];
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var provider = new ActiveLaunchProfileExtensionValueProvider(project, settingsProvider, threadingService);

            await provider.OnSetPropertyValueAsync(ActiveLaunchProfileExtensionValueProvider.AuthenticationModePropertyName, "NotWindows", Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "NotWindows", actual: activeProfileAuthenticationMode);
        }

        [Fact]
        public async Task NativeDebugging_OnGetEvaluatedPropertyValueAsync_GetsNativeDebuggingFromActiveProfile()
        {
            bool activeProfileNativeDebugging = true;
            var activeProfileOtherSettings = new Dictionary<string, object>
            {
                { LaunchProfileExtensions.NativeDebuggingProperty, activeProfileNativeDebugging }
            };

            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "One", activeProfileOtherSettings: activeProfileOtherSettings);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var provider = new ActiveLaunchProfileExtensionValueProvider(project, settingsProvider, threadingService);

            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileExtensionValueProvider.NativeDebuggingPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public async Task NativeDebugging_OnSetPropertyValueAsync_SetsDirectoryInActiveProfile()
        {
            bool activeProfileNativeDebugging = false;
            var activeProfileOtherSettings = new Dictionary<string, object>
            {
                { LaunchProfileExtensions.RemoteAuthenticationModeProperty, activeProfileNativeDebugging }
            };

            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "One",
                activeProfileOtherSettings: activeProfileOtherSettings,
                updateLaunchSettingsCallback: s =>
                {
                    Assumes.NotNull(s.ActiveProfile?.OtherSettings);
                    activeProfileNativeDebugging = (bool)s.ActiveProfile.OtherSettings[LaunchProfileExtensions.NativeDebuggingProperty];
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var provider = new ActiveLaunchProfileExtensionValueProvider(project, settingsProvider, threadingService);

            await provider.OnSetPropertyValueAsync(ActiveLaunchProfileExtensionValueProvider.NativeDebuggingPropertyName, "true", Mock.Of<IProjectProperties>());

            Assert.True(activeProfileNativeDebugging);
        }

        [Fact]
        public async Task RemoteDebugEnabled_OnGetEvaluatedPropertyValueAsync_GetsNativeDebuggingFromActiveProfile()
        {
            bool activeProfileRemoteDebugEnabled = true;
            var activeProfileOtherSettings = new Dictionary<string, object>
            {
                { LaunchProfileExtensions.RemoteDebugEnabledProperty, activeProfileRemoteDebugEnabled }
            };

            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "One", activeProfileOtherSettings: activeProfileOtherSettings);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var provider = new ActiveLaunchProfileExtensionValueProvider(project, settingsProvider, threadingService);

            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileExtensionValueProvider.RemoteDebugEnabledPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public async Task RemoveDebugEnabled_OnSetPropertyValueAsync_SetsDirectoryInActiveProfile()
        {
            bool activeProfileRemoteDebugEnabled = false;
            var activeProfileOtherSettings = new Dictionary<string, object>
            {
                { LaunchProfileExtensions.RemoteDebugEnabledProperty, activeProfileRemoteDebugEnabled }
            };

            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "One",
                activeProfileOtherSettings: activeProfileOtherSettings,
                updateLaunchSettingsCallback: s =>
                {
                    Assumes.NotNull(s.ActiveProfile?.OtherSettings);
                    activeProfileRemoteDebugEnabled = (bool)s.ActiveProfile.OtherSettings[LaunchProfileExtensions.RemoteDebugEnabledProperty];
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var provider = new ActiveLaunchProfileExtensionValueProvider(project, settingsProvider, threadingService);

            await provider.OnSetPropertyValueAsync(ActiveLaunchProfileExtensionValueProvider.RemoteDebugEnabledPropertyName, "true", Mock.Of<IProjectProperties>());

            Assert.True(activeProfileRemoteDebugEnabled);
        }

        [Fact]
        public async Task RemoteMachineName_OnGetEvaluatedPropertyValueAsync_GetsNameFromActiveProfile()
        {
            string activeProfileRemoteMachineName = "alphaMachine";
            var activeProfileOtherSettings = new Dictionary<string, object>
            {
                { LaunchProfileExtensions.RemoteDebugMachineProperty, activeProfileRemoteMachineName }
            };

            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "One", activeProfileOtherSettings: activeProfileOtherSettings);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var provider = new ActiveLaunchProfileExtensionValueProvider(project, settingsProvider, threadingService);

            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileExtensionValueProvider.RemoteDebugMachinePropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: activeProfileRemoteMachineName, actual: actualValue);
        }

        [Fact]
        public async Task RemoteMachineName_OnSetPropertyValueAsync_SetsDirectoryInActiveProfile()
        {
            string activeProfileRemoteMachineName = "Tiger";
            var activeProfileOtherSettings = new Dictionary<string, object>
            {
                { LaunchProfileExtensions.RemoteDebugMachineProperty, activeProfileRemoteMachineName }
            };

            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "One",
                activeProfileOtherSettings: activeProfileOtherSettings,
                updateLaunchSettingsCallback: s =>
                {
                    Assumes.NotNull(s.ActiveProfile?.OtherSettings);
                    activeProfileRemoteMachineName = (string)s.ActiveProfile.OtherSettings[LaunchProfileExtensions.RemoteDebugMachineProperty];
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var provider = new ActiveLaunchProfileExtensionValueProvider(project, settingsProvider, threadingService);

            await provider.OnSetPropertyValueAsync(ActiveLaunchProfileExtensionValueProvider.RemoteDebugMachinePropertyName, "Cheetah", Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "Cheetah", actual: activeProfileRemoteMachineName);
        }

        [Fact]
        public async Task SqlDebugEnabled_OnGetEvaluatedPropertyValueAsync_GetsSettingFromActiveProfile()
        {
            bool activeProfileSqlDebugEnabled = true;
            var activeProfileOtherSettings = new Dictionary<string, object>
            {
                { LaunchProfileExtensions.SqlDebuggingProperty, activeProfileSqlDebugEnabled }
            };

            var settingsProvider = SetupLaunchSettingsProvider(activeProfileName: "One", activeProfileOtherSettings: activeProfileOtherSettings);

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var provider = new ActiveLaunchProfileExtensionValueProvider(project, settingsProvider, threadingService);

            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(ActiveLaunchProfileExtensionValueProvider.SqlDebuggingPropertyName, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public async Task SqlDebugEnabled_OnSetPropertyValueAsync_SetsDirectoryInActiveProfile()
        {
            bool activeProfileSqlDebugEnabled = false;
            var activeProfileOtherSettings = new Dictionary<string, object>
            {
                { LaunchProfileExtensions.SqlDebuggingProperty, activeProfileSqlDebugEnabled }
            };

            var settingsProvider = SetupLaunchSettingsProvider(
                activeProfileName: "One",
                activeProfileOtherSettings: activeProfileOtherSettings,
                updateLaunchSettingsCallback: s =>
                {
                    Assumes.NotNull(s.ActiveProfile?.OtherSettings);
                    activeProfileSqlDebugEnabled = (bool)s.ActiveProfile.OtherSettings[LaunchProfileExtensions.SqlDebuggingProperty];
                });

            var project = UnconfiguredProjectFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var provider = new ActiveLaunchProfileExtensionValueProvider(project, settingsProvider, threadingService);

            await provider.OnSetPropertyValueAsync(ActiveLaunchProfileExtensionValueProvider.SqlDebuggingPropertyName, "true", Mock.Of<IProjectProperties>());

            Assert.True(activeProfileSqlDebugEnabled);
        }

        private static ILaunchSettingsProvider SetupLaunchSettingsProvider(
            string activeProfileName,
            string? activeProfileLaunchTarget = null,
            string? activeProfileExecutablePath = null,
            string? activeProfileCommandLineArgs = null,
            string? activeProfileWorkingDirectory = null,
            bool? activeProfileLaunchBrowser = null,
            string? activeProfileLaunchUrl = null,
            Dictionary<string, object>? activeProfileOtherSettings = null,
            Action<string>? setActiveProfileCallback = null,
            Action<ILaunchSettings>? updateLaunchSettingsCallback = null)
        {
            var profile = new WritableLaunchProfile
            {
                Name = activeProfileName,
                CommandName = activeProfileLaunchTarget,
                ExecutablePath = activeProfileExecutablePath
            };

            if (activeProfileLaunchTarget != null)
            {
                profile.CommandName = activeProfileLaunchTarget;
            }

            if (activeProfileExecutablePath != null)
            {
                profile.ExecutablePath = activeProfileExecutablePath;
            }

            if (activeProfileCommandLineArgs != null)
            {
                profile.CommandLineArgs = activeProfileCommandLineArgs;
            }

            if (activeProfileWorkingDirectory != null)
            {
                profile.WorkingDirectory = activeProfileWorkingDirectory;
            }

            if (activeProfileLaunchBrowser != null)
            {
                profile.LaunchBrowser = activeProfileLaunchBrowser.Value;
            }

            if (activeProfileLaunchUrl != null)
            {
                profile.LaunchUrl = activeProfileLaunchUrl;
            }

            if (activeProfileOtherSettings != null)
            {
                Assumes.NotNull(profile.OtherSettings);

                foreach ((string key, object value) in activeProfileOtherSettings)
                {
                    profile.OtherSettings.Add(key, value);
                }
            }

            var settingsProvider = ILaunchSettingsProviderFactory.Create(
                activeProfileName,
                new[] { profile.ToLaunchProfile() },
                updateLaunchSettingsCallback: updateLaunchSettingsCallback,
                setActiveProfileCallback: setActiveProfileCallback);
            return settingsProvider;
        }
    }
}
