// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class ProjectLaunchProfileExtensionValueProviderTests
    {
        private static readonly ImmutableDictionary<string, object> EmptyGlobalSettings = ImmutableDictionary<string, object>.Empty;

        [Fact]
        public async Task AuthenticationMode_OnGetPropertyValueAsync_GetsModeFromActiveProfile()
        {
            string activeProfileAuthenticationMode = "Windows";
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.RemoteAuthenticationModeProperty, activeProfileAuthenticationMode }
                }
            }.ToLaunchProfile();

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            var actualValue = await provider.OnGetPropertyValueAsync(ProjectLaunchProfileExtensionValueProvider.AuthenticationModePropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: activeProfileAuthenticationMode, actual: actualValue);
        }

        [Fact]
        public async Task AuthenticationMode_OnSetPropertyValueAsync_SetsModeInActiveProfile()
        {
            string activeProfileAuthenticationMode = "Windows";
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.RemoteAuthenticationModeProperty, activeProfileAuthenticationMode }
                }
            };

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            await provider.OnSetPropertyValueAsync(ProjectLaunchProfileExtensionValueProvider.AuthenticationModePropertyName, "NotWindows", profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "NotWindows", actual: profile.OtherSettings[LaunchProfileExtensions.RemoteAuthenticationModeProperty]);
        }

        [Fact]
        public async Task NativeDebugging_OnGetPropertyValueAsync_GetsNativeDebuggingFromActiveProfile()
        {
            bool activeProfileNativeDebugging = true;
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.NativeDebuggingProperty, activeProfileNativeDebugging }
                }
            }.ToLaunchProfile();

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            var actualValue = await provider.OnGetPropertyValueAsync(ProjectLaunchProfileExtensionValueProvider.NativeDebuggingPropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public async Task NativeDebugging_OnSetPropertyValueAsync_SetsNativeDebuggingInActiveProfile()
        {
            bool activeProfileNativeDebugging = false;
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.NativeDebuggingProperty, activeProfileNativeDebugging }
                }
            };

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            await provider.OnSetPropertyValueAsync(ProjectLaunchProfileExtensionValueProvider.NativeDebuggingPropertyName, "true", profile, EmptyGlobalSettings, rule: null);

            Assert.True((bool)profile.OtherSettings[LaunchProfileExtensions.NativeDebuggingProperty]);
        }

        [Fact]
        public async Task RemoteDebugEnabled_OnGetPropertyValueAsync_GetsRemoteDebuggingFromActiveProfile()
        {
            bool activeProfileRemoteDebugEnabled = true;
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.RemoteDebugEnabledProperty, activeProfileRemoteDebugEnabled }
                }
            }.ToLaunchProfile();

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            var actualValue = await provider.OnGetPropertyValueAsync(ProjectLaunchProfileExtensionValueProvider.RemoteDebugEnabledPropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public async Task RemoteDebugEnabled_OnSetPropertyValueAsync_SetsRemoteDebuggingInActiveProfile()
        {
            bool activeProfileRemoteDebugEnabled = false;
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.RemoteDebugEnabledProperty, activeProfileRemoteDebugEnabled }
                }
            };

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            await provider.OnSetPropertyValueAsync(ProjectLaunchProfileExtensionValueProvider.RemoteDebugEnabledPropertyName, "true", profile, EmptyGlobalSettings, rule: null);

            Assert.True((bool)profile.OtherSettings[LaunchProfileExtensions.RemoteDebugEnabledProperty]);
        }

        [Fact]
        public async Task RemoteMachineName_OnGetPropertyValueAsync_GetsNameFromActiveProfile()
        {
            string activeProfileRemoteMachineName = "alphaMachine";
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.RemoteDebugMachineProperty, activeProfileRemoteMachineName }
                }
            }.ToLaunchProfile();

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            var actualValue = await provider.OnGetPropertyValueAsync(ProjectLaunchProfileExtensionValueProvider.RemoteDebugMachinePropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: activeProfileRemoteMachineName, actual: actualValue);
        }

        [Fact]
        public async Task RemoteMachineName_OnSetPropertyValueAsync_SetsNameInActiveProfile()
        {
            string activeProfileRemoteMachineName = "Tiger";
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.RemoteDebugMachineProperty, activeProfileRemoteMachineName }
                }
            };

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            await provider.OnSetPropertyValueAsync(ProjectLaunchProfileExtensionValueProvider.RemoteDebugMachinePropertyName, "Cheetah", profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "Cheetah", actual: profile.OtherSettings[LaunchProfileExtensions.RemoteDebugMachineProperty]);
        }

        [Fact]
        public async Task SqlDebugEnabled_OnGetPropertyValueAsync_GetsSettingFromActiveProfile()
        {
            bool activeProfileSqlDebugEnabled = true;
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.SqlDebuggingProperty, activeProfileSqlDebugEnabled }
                }
            }.ToLaunchProfile();

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            var actualValue = await provider.OnGetPropertyValueAsync(ProjectLaunchProfileExtensionValueProvider.SqlDebuggingPropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public async Task SqlDebugEnabled_OnSetPropertyValueAsync_SetsSqlDebugInActiveProfile()
        {
            bool activeProfileSqlDebugEnabled = false;
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.SqlDebuggingProperty, activeProfileSqlDebugEnabled }
                }
            };

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            await provider.OnSetPropertyValueAsync(ProjectLaunchProfileExtensionValueProvider.SqlDebuggingPropertyName, "true", profile, EmptyGlobalSettings, rule: null);

            Assert.True((bool)profile.OtherSettings[LaunchProfileExtensions.SqlDebuggingProperty]);
        }
    }
}
