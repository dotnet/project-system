// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class ProjectLaunchProfileExtensionValueProviderTests
    {
        private static readonly ImmutableDictionary<string, object> EmptyGlobalSettings = ImmutableDictionary<string, object>.Empty;

        [Fact]
        public void AuthenticationMode_OnGetPropertyValueAsync_GetsModeFromActiveProfile()
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

            var actualValue = provider.OnGetPropertyValue(ProjectLaunchProfileExtensionValueProvider.AuthenticationModePropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: activeProfileAuthenticationMode, actual: actualValue);
        }

        [Fact]
        public void AuthenticationMode_OnSetPropertyValueAsync_SetsModeInActiveProfile()
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

            provider.OnSetPropertyValue(ProjectLaunchProfileExtensionValueProvider.AuthenticationModePropertyName, "NotWindows", profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "NotWindows", actual: profile.OtherSettings[LaunchProfileExtensions.RemoteAuthenticationModeProperty]);
        }

        [Fact]
        public void NativeDebugging_OnGetPropertyValueAsync_GetsNativeDebuggingFromActiveProfile()
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

            var actualValue = provider.OnGetPropertyValue(ProjectLaunchProfileExtensionValueProvider.NativeDebuggingPropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public void NativeDebugging_OnSetPropertyValueAsync_SetsNativeDebuggingInActiveProfile()
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

            provider.OnSetPropertyValue(ProjectLaunchProfileExtensionValueProvider.NativeDebuggingPropertyName, "true", profile, EmptyGlobalSettings, rule: null);

            Assert.True((bool)profile.OtherSettings[LaunchProfileExtensions.NativeDebuggingProperty]);
        }

        [Fact]
        public void RemoteDebugEnabled_OnGetPropertyValueAsync_GetsRemoteDebuggingFromActiveProfile()
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

            var actualValue = provider.OnGetPropertyValue(ProjectLaunchProfileExtensionValueProvider.RemoteDebugEnabledPropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public void RemoteDebugEnabled_OnSetPropertyValueAsync_SetsRemoteDebuggingInActiveProfile()
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

            provider.OnSetPropertyValue(ProjectLaunchProfileExtensionValueProvider.RemoteDebugEnabledPropertyName, "true", profile, EmptyGlobalSettings, rule: null);

            Assert.True((bool)profile.OtherSettings[LaunchProfileExtensions.RemoteDebugEnabledProperty]);
        }

        [Fact]
        public void RemoteMachineName_OnGetPropertyValueAsync_GetsNameFromActiveProfile()
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

            var actualValue = provider.OnGetPropertyValue(ProjectLaunchProfileExtensionValueProvider.RemoteDebugMachinePropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: activeProfileRemoteMachineName, actual: actualValue);
        }

        [Fact]
        public void RemoteMachineName_OnSetPropertyValueAsync_SetsNameInActiveProfile()
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

            provider.OnSetPropertyValue(ProjectLaunchProfileExtensionValueProvider.RemoteDebugMachinePropertyName, "Cheetah", profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "Cheetah", actual: profile.OtherSettings[LaunchProfileExtensions.RemoteDebugMachineProperty]);
        }

        [Fact]
        public void SqlDebugEnabled_OnGetPropertyValueAsync_GetsSettingFromActiveProfile()
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

            var actualValue = provider.OnGetPropertyValue(ProjectLaunchProfileExtensionValueProvider.SqlDebuggingPropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public void SqlDebugEnabled_OnSetPropertyValueAsync_SetsSqlDebugInActiveProfile()
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

            provider.OnSetPropertyValue(ProjectLaunchProfileExtensionValueProvider.SqlDebuggingPropertyName, "true", profile, EmptyGlobalSettings, rule: null);

            Assert.True((bool)profile.OtherSettings[LaunchProfileExtensions.SqlDebuggingProperty]);
        }

        [Fact]
        public void HotReloadEnabled_OnGetPropertyValueAsync_GetsDefaultValueWhenNotDefined()
        {
            var profile = new WritableLaunchProfile().ToLaunchProfile();

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            var actualValue = provider.OnGetPropertyValue(ProjectLaunchProfileExtensionValueProvider.HotReloadEnabledPropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public void HotReloadEnabled_OnGetPropertyValueAsync_GetsValueInProfileWhenDefined()
        {
            bool hotReloadEnabled = false;
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.HotReloadEnabledProperty, hotReloadEnabled }
                }
            }.ToLaunchProfile();

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            var actualValue = provider.OnGetPropertyValue(ProjectLaunchProfileExtensionValueProvider.HotReloadEnabledPropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "false", actual: actualValue);
        }

        [Fact]
        public void HotReloadEnabled_OnSetPropertyValueAsync_SetsHotReloadToSpecifiedValue()
        {
            bool hotReloadEnabled = true;
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.HotReloadEnabledProperty, hotReloadEnabled }
                }
            };

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            provider.OnSetPropertyValue(ProjectLaunchProfileExtensionValueProvider.HotReloadEnabledPropertyName, "false", profile, EmptyGlobalSettings, rule: null);

            Assert.False((bool)profile.OtherSettings[LaunchProfileExtensions.HotReloadEnabledProperty]);
        }

        [Fact]
        public void WebView2Debugging_OnGetPropertyValueAsync_GetsDefaultValueWhenNotDefined()
        {
            var profile = new WritableLaunchProfile().ToLaunchProfile();

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            var actualValue = provider.OnGetPropertyValue(ProjectLaunchProfileExtensionValueProvider.WebView2DebuggingPropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "false", actual: actualValue);
        }

        [Fact]
        public void WebView2Debugging_OnGetPropertyValueAsync_GetsValueInProfileWhenDefined()
        {
            bool webView2Debugging = true;
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.JSWebView2DebuggingProperty, webView2Debugging }
                }
            }.ToLaunchProfile();

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            var actualValue = provider.OnGetPropertyValue(ProjectLaunchProfileExtensionValueProvider.WebView2DebuggingPropertyName, profile, EmptyGlobalSettings, rule: null);

            Assert.Equal(expected: "true", actual: actualValue);
        }

        [Fact]
        public void WebView2Debugging_OnSetPropertyValueAsync_SetsWebView2DebuggingToSpecifiedValue()
        {
            bool webView2Debugging = false;
            var profile = new WritableLaunchProfile
            {
                OtherSettings =
                {
                    { LaunchProfileExtensions.JSWebView2DebuggingProperty, webView2Debugging }
                }
            };

            var provider = new ProjectLaunchProfileExtensionValueProvider();

            provider.OnSetPropertyValue(ProjectLaunchProfileExtensionValueProvider.WebView2DebuggingPropertyName, "true", profile, EmptyGlobalSettings, rule: null);

            Assert.True((bool)profile.OtherSettings[LaunchProfileExtensions.JSWebView2DebuggingProperty]);
        }
    }
}
