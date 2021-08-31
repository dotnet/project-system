// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

        /// <summary>
        /// Returns true if nativeDebugging property is set to true
        /// </summary>
        public static bool IsNativeDebuggingEnabled(this ILaunchProfile profile)
        {
            if (profile.OtherSettings != null
                && profile.OtherSettings.TryGetValue(NativeDebuggingProperty, out object? value)
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
            if (profile.OtherSettings != null
                && profile.OtherSettings.TryGetValue(SqlDebuggingProperty, out object? value)
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
            if (profile.OtherSettings != null
                && profile.OtherSettings.TryGetValue(JSWebView2DebuggingProperty, out object? value)
                && value is bool b)
            {
                return b;
            }

            return false;
        }

        public static bool IsRemoteDebugEnabled(this ILaunchProfile profile)
        {
            if (profile.OtherSettings != null
                && profile.OtherSettings.TryGetValue(RemoteDebugEnabledProperty, out object? value)
                && value is bool b)
            {
                return b;
            }

            return false;
        }

        public static string? RemoteDebugMachine(this ILaunchProfile profile)
        {
            if (profile.OtherSettings != null
                && profile.OtherSettings.TryGetValue(RemoteDebugMachineProperty, out object? value)
                && value is string s)
            {
                return s;
            }

            return null;
        }

        public static string? RemoteAuthenticationMode(this ILaunchProfile profile)
        {
            if (profile?.OtherSettings != null
               && profile.OtherSettings.TryGetValue(RemoteAuthenticationModeProperty, out object? value)
               && value is string s)
            {
                return s;
            }

            return null;
        }

        public static bool IsHotReloadEnabled(this ILaunchProfile profile)
        {
            if (profile.OtherSettings is not null
                && profile.OtherSettings.TryGetValue(HotReloadEnabledProperty, out object? value)
                && value is bool b)
            {
                return b;
            }

            return true;
        }
    }
}
