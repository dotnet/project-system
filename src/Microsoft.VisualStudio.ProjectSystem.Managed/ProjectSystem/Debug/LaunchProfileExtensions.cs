// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal static class LaunchProfileExtensions
    {
        public const string HotReloadEnabledProperty = "hotReloadEnabled";
        public const string NativeDebuggingProperty = "nativeDebugging";
        public const string SqlDebuggingProperty = "sqlDebugging";
        public const string JSWebView2DebuggingProperty = "jsWebView2Debugging";
        public const string RemoteDebugEnabledProperty = "remoteDebugEnabled";
        public const string RemoteDebugMachineProperty = "remoteDebugMachine";
        public const string RemoteAuthenticationModeProperty = "authenticationMode";

        public static bool IsInMemoryObject(this object persistObject)
        {
            return persistObject is IPersistOption profile2 && profile2.DoNotPersist;
        }

        public static bool TryGetSetting(this ILaunchProfile profile, string key, [NotNullWhen(returnValue: true)] out object? value)
        {
            if (profile is ILaunchProfile2 lp2)
            {
                foreach ((string k, object v) in lp2.OtherSettings)
                {
                    if (StringComparers.LaunchSettingsPropertyNames.Equals(key, k))
                    {
                        value = v;
                        return true;
                    }
                }
            }

            if (profile.OtherSettings?.TryGetValue(key, out value) is true)
            {
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Returns true if nativeDebugging property is set to true
        /// </summary>
        public static bool IsNativeDebuggingEnabled(this ILaunchProfile profile)
        {
            if (profile.TryGetSetting(NativeDebuggingProperty, out object? value)
                && value is bool b)
            {
                return b;
            }

            return false;
        }

        /// <summary>
        /// Returns true if sqlDebugging property is set to true
        /// </summary>
        public static bool IsSqlDebuggingEnabled(this ILaunchProfile profile)
        {
            if (profile.TryGetSetting(SqlDebuggingProperty, out object? value)
                && value is bool b)
            {
                return b;
            }

            return false;
        }

        /// <summary>
        /// Returns true if jsWebView2Debugging property is set to true
        /// </summary>
        public static bool IsJSWebView2DebuggingEnabled(this ILaunchProfile profile)
        {
            if (profile.TryGetSetting(JSWebView2DebuggingProperty, out object? value)
                && value is bool b)
            {
                return b;
            }

            return false;
        }

        public static bool IsRemoteDebugEnabled(this ILaunchProfile profile)
        {
            if (profile.TryGetSetting(RemoteDebugEnabledProperty, out object? value)
                && value is bool b)
            {
                return b;
            }

            return false;
        }

        public static string? RemoteDebugMachine(this ILaunchProfile profile)
        {
            if (profile.TryGetSetting(RemoteDebugMachineProperty, out object? value)
                && value is string s)
            {
                return s;
            }

            return null;
        }

        public static string? RemoteAuthenticationMode(this ILaunchProfile profile)
        {
            if (profile.TryGetSetting(RemoteAuthenticationModeProperty, out object? value)
               && value is string s)
            {
                return s;
            }

            return null;
        }

        public static bool IsHotReloadEnabled(this ILaunchProfile profile)
        {
            if (profile.TryGetSetting(HotReloadEnabledProperty, out object? value)
                && value is bool b)
            {
                return b;
            }

            return true;
        }

        /// <summary>
        /// Enumerates the profile's environment variables, preserving order if possible.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <paramref name="profile"/> is <see cref="ILaunchProfile2"/>, we enumerate
        /// <see cref="ILaunchProfile2.EnvironmentVariables"/> directly which has an
        /// explicit order.
        /// </para>
        /// <para>
        /// If <paramref name="profile"/> is <see cref="ILaunchProfile"/>,
        /// <see cref="ILaunchProfile.EnvironmentVariables"/> is unordered (hash ordered),
        /// so we order by key.
        /// </para>
        /// </remarks>
        /// <param name="profile">The profile to read from.</param>
        /// <returns>An ordered enumeration of environment variable name/value pairs.</returns>
        public static IEnumerable<(string Key, string Value)> EnumerateEnvironmentVariables(this ILaunchProfile profile)
        {
            return profile switch
            {
                ILaunchProfile2 launchProfile => launchProfile.EnvironmentVariables,
                ILaunchProfile { EnvironmentVariables: null or { Count: 0 } } => Enumerable.Empty<(string Key, string Value)>(),
                ILaunchProfile { EnvironmentVariables: { } vars } => vars.OrderBy(pair => pair.Key, StringComparers.EnvironmentVariableNames).Select(pair => (pair.Key, pair.Value))
            };
        }

        /// <summary>
        /// Enumerates the profile's other settings, preserving order if possible.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <paramref name="profile"/> is <see cref="ILaunchProfile2"/>, we enumerate
        /// <see cref="ILaunchProfile2.OtherSettings"/> directly which has an
        /// explicit order.
        /// </para>
        /// <para>
        /// If <paramref name="profile"/> is <see cref="ILaunchProfile"/>,
        /// <see cref="ILaunchProfile.OtherSettings"/> is unordered (hash ordered),
        /// so we order by key.
        /// </para>
        /// </remarks>
        /// <param name="profile">The profile to read from.</param>
        /// <returns>An ordered enumeration of other setting name/value pairs.</returns>
        public static IEnumerable<(string Key, object Value)> EnumerateOtherSettings(this ILaunchProfile profile)
        {
            return profile switch
            {
                ILaunchProfile2 launchProfile => launchProfile.OtherSettings,
                ILaunchProfile { OtherSettings: null or { Count: 0 } } => Enumerable.Empty<(string Key, object Value)>(),
                ILaunchProfile { OtherSettings: { } settings } => settings.OrderBy(pair => pair.Key, StringComparers.LaunchProfileProperties).Select(pair => (pair.Key, pair.Value))
            };
        }

        public static Dictionary<string, string>? GetEnvironmentVariablesDictionary(this ILaunchProfile profile)
        {
            return profile switch
            {
                ILaunchProfile2 launchProfile => ToDictionary(launchProfile.EnvironmentVariables, StringComparers.EnvironmentVariableNames),
                ILaunchProfile { EnvironmentVariables: null or { Count: 0 } } => null,
                ILaunchProfile { EnvironmentVariables: { } vars } => vars.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparers.EnvironmentVariableNames)
            };
        }

        public static Dictionary<string, object>? GetOtherSettingsDictionary(this ILaunchProfile profile)
        {
            return profile switch
            {
                ILaunchProfile2 launchProfile => ToDictionary(launchProfile.OtherSettings, StringComparers.LaunchProfileProperties),
                ILaunchProfile { OtherSettings: null or { Count: 0 } } => null,
                ILaunchProfile { OtherSettings: { } vars } => vars.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparers.LaunchProfileProperties)
            };
        }

        private static Dictionary<string, T>? ToDictionary<T>(ImmutableArray<(string, T)> source, IEqualityComparer<string> comparer)
        {
            if (source.IsEmpty)
            {
                return null;
            }

            var result = new Dictionary<string, T>(capacity: source.Length, comparer);

            foreach ((string key, T value) in source)
            {
                result[key] = value;
            }

            return result;
        }

        /// <summary>
        /// Produces an immutable array of environment variable name/value pairs, preserving order if possible.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <paramref name="profile"/> is <see cref="ILaunchProfile2"/>, we return
        /// <see cref="ILaunchProfile2.EnvironmentVariables"/> directly which has an
        /// explicit order.
        /// </para>
        /// <para>
        /// If <paramref name="profile"/> is <see cref="ILaunchProfile"/>,
        /// <see cref="ILaunchProfile.EnvironmentVariables"/> is unordered (hash ordered),
        /// We allocate a new immutable array, then populate it with name/value pairs, in name order.
        /// </para>
        /// </remarks>
        /// <param name="profile">The profile to read from.</param>
        /// <returns>An immutable array of environment variable name/value pairs.</returns>
        public static ImmutableArray<(string Key, string Value)> FlattenEnvironmentVariables(this ILaunchProfile profile)
        {
            return profile switch
            {
                ILaunchProfile2 launchProfile => launchProfile.EnvironmentVariables,
                ILaunchProfile { EnvironmentVariables: null or { Count: 0 } } => ImmutableArray<(string Key, string Value)>.Empty,
                ILaunchProfile { EnvironmentVariables: { } vars } => vars.OrderBy(pair => pair.Key, StringComparers.EnvironmentVariableNames).Select(pair => (pair.Key, pair.Value)).ToImmutableArray()
            };
        }

        /// <summary>
        /// Produces an immutable array of other setting name/value pairs, preserving order if possible.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <paramref name="profile"/> is <see cref="ILaunchProfile2"/>, we return
        /// <see cref="ILaunchProfile2.OtherSettings"/> directly which has an
        /// explicit order.
        /// </para>
        /// <para>
        /// If <paramref name="profile"/> is <see cref="ILaunchProfile"/>,
        /// <see cref="ILaunchProfile.OtherSettings"/> is unordered (hash ordered),
        /// We allocate a new immutable array, then populate it with name/value pairs, in name order.
        /// </para>
        /// </remarks>
        /// <param name="profile">The profile to read from.</param>
        /// <returns>An immutable array of other setting name/value pairs.</returns>
        public static ImmutableArray<(string Key, object Value)> FlattenOtherSettings(this ILaunchProfile profile)
        {
            return profile switch
            {
                ILaunchProfile2 launchProfile => launchProfile.OtherSettings,
                ILaunchProfile { OtherSettings: null or { Count: 0 } } => ImmutableArray<(string Key, object Value)>.Empty,
                ILaunchProfile { OtherSettings: { } vars } => vars.OrderBy(pair => pair.Key, StringComparers.EnvironmentVariableNames).Select(pair => (pair.Key, pair.Value)).ToImmutableArray()
            };
        }
    }
}
