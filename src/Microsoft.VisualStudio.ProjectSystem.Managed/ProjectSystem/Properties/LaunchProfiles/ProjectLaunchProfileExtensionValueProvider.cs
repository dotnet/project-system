// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// <para>
    /// Reads and writes "extension" properties in the given <see cref="ILaunchProfile"/>.
    /// </para>
    /// <para>"Extension" means properties that are stored in the <see cref="ILaunchProfile.OtherSettings"/>
    /// dictionary, rather than in named properties of <see cref="ILaunchProfile"/>
    /// itself. Those are handled by <see cref="LaunchProfileProjectProperties"/>.
    /// </para>
    /// </summary>
    [ExportLaunchProfileExtensionValueProvider(
        new[]
        {
            AuthenticationModePropertyName,
            HotReloadEnabledPropertyName,
            NativeDebuggingPropertyName,
            RemoteDebugEnabledPropertyName,
            RemoteDebugMachinePropertyName,
            SqlDebuggingPropertyName,
            WebView2DebuggingPropertyName
        },
        ExportLaunchProfileExtensionValueProviderScope.LaunchProfile)]
    internal class ProjectLaunchProfileExtensionValueProvider : ILaunchProfileExtensionValueProvider
    {
        internal const string AuthenticationModePropertyName = "AuthenticationMode";
        internal const string HotReloadEnabledPropertyName = "HotReloadEnabled";
        internal const string NativeDebuggingPropertyName = "NativeDebugging";
        internal const string RemoteDebugEnabledPropertyName = "RemoteDebugEnabled";
        internal const string RemoteDebugMachinePropertyName = "RemoteDebugMachine";
        internal const string SqlDebuggingPropertyName = "SqlDebugging";
        internal const string WebView2DebuggingPropertyName = "WebView2Debugging";

        // The CPS property system will map "true" and "false" to the localized versions of
        // "Yes" and "No" for display purposes, but not other casings like "True" and
        // "False". To ensure consistency we need to map booleans to these constants.
        private const string True = "true";
        private const string False = "false";

        public string OnGetPropertyValue(string propertyName, ILaunchProfile launchProfile, ImmutableDictionary<string, object> globalSettings, Rule? rule)
        {
            string propertyValue = propertyName switch
            {
                AuthenticationModePropertyName => GetOtherProperty(launchProfile, LaunchProfileExtensions.RemoteAuthenticationModeProperty, string.Empty),
                HotReloadEnabledPropertyName => GetOtherProperty(launchProfile, LaunchProfileExtensions.HotReloadEnabledProperty, true) ? True : False,
                NativeDebuggingPropertyName => GetOtherProperty(launchProfile, LaunchProfileExtensions.NativeDebuggingProperty, false) ? True : False,
                RemoteDebugEnabledPropertyName => GetOtherProperty(launchProfile, LaunchProfileExtensions.RemoteDebugEnabledProperty, false) ? True : False,
                RemoteDebugMachinePropertyName => GetOtherProperty(launchProfile, LaunchProfileExtensions.RemoteDebugMachineProperty, string.Empty),
                SqlDebuggingPropertyName => GetOtherProperty(launchProfile, LaunchProfileExtensions.SqlDebuggingProperty, false) ? True : False,
                WebView2DebuggingPropertyName => GetOtherProperty(launchProfile, LaunchProfileExtensions.JSWebView2DebuggingProperty, false) ? True : False,

                _ => throw new InvalidOperationException($"{nameof(ProjectLaunchProfileExtensionValueProvider)} does not handle property '{propertyName}'.")
            };

            return propertyValue;
        }

        public void OnSetPropertyValue(string propertyName, string propertyValue, IWritableLaunchProfile launchProfile, ImmutableDictionary<string, object> globalSettings, Rule? rule)
        {
            // TODO: Should the result (success or failure) be ignored?
            _ = propertyName switch
            {
                AuthenticationModePropertyName => TrySetOtherProperty(launchProfile, LaunchProfileExtensions.RemoteAuthenticationModeProperty, propertyValue, string.Empty),
                HotReloadEnabledPropertyName =>   TrySetOtherProperty(launchProfile, LaunchProfileExtensions.HotReloadEnabledProperty, bool.Parse(propertyValue), true),
                NativeDebuggingPropertyName =>    TrySetOtherProperty(launchProfile, LaunchProfileExtensions.NativeDebuggingProperty, bool.Parse(propertyValue), false),
                RemoteDebugEnabledPropertyName => TrySetOtherProperty(launchProfile, LaunchProfileExtensions.RemoteDebugEnabledProperty, bool.Parse(propertyValue), false),
                RemoteDebugMachinePropertyName => TrySetOtherProperty(launchProfile, LaunchProfileExtensions.RemoteDebugMachineProperty, propertyValue, string.Empty),
                SqlDebuggingPropertyName =>       TrySetOtherProperty(launchProfile, LaunchProfileExtensions.SqlDebuggingProperty, bool.Parse(propertyValue), false),
                WebView2DebuggingPropertyName =>  TrySetOtherProperty(launchProfile, LaunchProfileExtensions.JSWebView2DebuggingProperty, bool.Parse(propertyValue), false),
                _ => throw new InvalidOperationException($"{nameof(ProjectLaunchProfileExtensionValueProvider)} does not handle property '{propertyName}'."),
            };
        }

        private static T GetOtherProperty<T>(ILaunchProfile launchProfile, string propertyName, T defaultValue)
        {
            if (launchProfile.TryGetSetting(propertyName, out object? value))
            {
                if (value is T b)
                {
                    return b;
                }

                if (value is string s &&
                    TypeDescriptor.GetConverter(typeof(T)) is TypeConverter converter &&
                    converter.CanConvertFrom(typeof(string)))
                {
                    try
                    {
                        if (converter.ConvertFromString(s) is T o)
                        {
                            return o;
                        }
                    }
                    catch (Exception)
                    {
                        // ignore bad data in the json file and just let them have the default value
                    }
                }
            }

            return defaultValue;
        }

        private static bool TrySetOtherProperty<T>(IWritableLaunchProfile launchProfile, string propertyName, T value, T defaultValue) where T : notnull
        {
            if (!launchProfile.OtherSettings.TryGetValue(propertyName, out object current))
            {
                current = defaultValue;
            }

            if (current is not T currentTyped || !Equals(currentTyped, value))
            {
                launchProfile.OtherSettings[propertyName] = value;
                return true;
            }

            return false;
        }
    }
}
