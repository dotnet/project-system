// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// <para>
    /// Reads and writes "extension" properties of the active <see cref="ILaunchProfile"/>
    /// via the <see cref="ILaunchSettingsProvider"/>.
    /// </para>
    /// <para>"Extension" means properties that are stored in the <see cref="ILaunchProfile.OtherSettings"/>
    /// dictionary, rather than in named properties of <see cref="ILaunchProfile"/>
    /// itself. Those are handled by <see cref="ActiveLaunchProfileCommonValueProvider"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Not to be confused with <see cref="ActiveLaunchProfileNameValueProvider" />,
    /// which reads and writes the _name_ of the active launch profile but does not
    /// access its members.
    /// </remarks>
    [ExportInterceptingPropertyValueProvider(
        new[]
        {
            AuthenticationModePropertyName,
            NativeDebuggingPropertyName,
            RemoteDebugEnabledPropertyName,
            RemoteDebugMachinePropertyName,
            SqlDebuggingPropertyName
        },
        ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class ActiveLaunchProfileExtensionValueProvider : LaunchSettingsValueProviderBase
    {
        internal const string AuthenticationModePropertyName = "AuthenticationMode";
        internal const string NativeDebuggingPropertyName = "NativeDebugging";
        internal const string RemoteDebugEnabledPropertyName = "RemoteDebugEnabled";
        internal const string RemoteDebugMachinePropertyName = "RemoteDebugMachine";
        internal const string SqlDebuggingPropertyName = "SqlDebugging";

        // The CPS property system will map "true" and "false" to the localized versions of
        // "Yes" and "No" for display purposes, but not other casings like "True" and
        // "False". To ensure consistency we need to map booleans to these constants.
        private const string True = "true";
        private const string False = "false";

        [ImportingConstructor]
        public ActiveLaunchProfileExtensionValueProvider(ILaunchSettingsProvider launchSettingsProvider)
            : base(launchSettingsProvider)
        {
        }

        public override string? GetPropertyValue(string propertyName, ILaunchSettings launchSettings)
        {
            ILaunchProfile? activeProfile = launchSettings.ActiveProfile;

            string activeProfilePropertyValue = propertyName switch
            {
                AuthenticationModePropertyName => GetOtherProperty(activeProfile, LaunchProfileExtensions.RemoteAuthenticationModeProperty, string.Empty),
                NativeDebuggingPropertyName => GetOtherProperty(activeProfile, LaunchProfileExtensions.NativeDebuggingProperty, false) ? True : False,
                RemoteDebugEnabledPropertyName => GetOtherProperty(activeProfile, LaunchProfileExtensions.RemoteDebugEnabledProperty, false) ? True : False,
                RemoteDebugMachinePropertyName => GetOtherProperty(activeProfile, LaunchProfileExtensions.RemoteDebugMachineProperty, string.Empty),
                SqlDebuggingPropertyName => GetOtherProperty(activeProfile, LaunchProfileExtensions.SqlDebuggingProperty, false) ? True : False,

                _ => throw new InvalidOperationException($"{nameof(ActiveLaunchProfileExtensionValueProvider)} does not handle property '{propertyName}'.")
            };

            return activeProfilePropertyValue;
        }

        public override bool SetPropertyValue(string propertyName, string value, IWritableLaunchSettings launchSettings)
        {
            IWritableLaunchProfile? activeProfile = launchSettings.ActiveProfile;
            if (activeProfile == null)
            {
                return false;
            }

            UpdateActiveLaunchProfile(activeProfile, propertyName, value);

            return true;
        }

        private static void UpdateActiveLaunchProfile(IWritableLaunchProfile activeProfile, string propertyName, string unevaluatedPropertyValue)
        {
            switch (propertyName)
            {
                case AuthenticationModePropertyName:
                    TrySetOtherProperty(activeProfile, LaunchProfileExtensions.RemoteAuthenticationModeProperty, unevaluatedPropertyValue, string.Empty);
                    break;

                case NativeDebuggingPropertyName:
                    TrySetOtherProperty(activeProfile, LaunchProfileExtensions.NativeDebuggingProperty, bool.Parse(unevaluatedPropertyValue), false);
                    break;

                case RemoteDebugEnabledPropertyName:
                    TrySetOtherProperty(activeProfile, LaunchProfileExtensions.RemoteDebugEnabledProperty, bool.Parse(unevaluatedPropertyValue), false);
                    break;

                case RemoteDebugMachinePropertyName:
                    TrySetOtherProperty(activeProfile, LaunchProfileExtensions.RemoteDebugMachineProperty, unevaluatedPropertyValue, string.Empty);
                    break;

                case SqlDebuggingPropertyName:
                    TrySetOtherProperty(activeProfile, LaunchProfileExtensions.SqlDebuggingProperty, bool.Parse(unevaluatedPropertyValue), false);
                    break;

                default:
                    throw new InvalidOperationException($"{nameof(ActiveLaunchProfileExtensionValueProvider)} does not handle property '{propertyName}'.");
            }
        }

        private static T GetOtherProperty<T>(ILaunchProfile? launchProfile, string propertyName, T defaultValue)
        {
            if (launchProfile == null
                || launchProfile.OtherSettings == null)
            {
                return defaultValue;
            }

            if (launchProfile.OtherSettings.TryGetValue(propertyName, out object? value) &&
                value is T b)
            {
                return b;
            }
            else if (value is string s &&
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
