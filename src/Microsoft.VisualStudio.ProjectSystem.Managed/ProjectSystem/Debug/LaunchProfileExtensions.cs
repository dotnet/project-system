// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal static class LaunchProfileExtensions
    {
        public const string NativeDebuggingProperty = "nativeDebugging";
        public const string SqlDebuggingProperty = "sqlDebugging";

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
                && profile.OtherSettings.TryGetValue(NativeDebuggingProperty, out object nativeDebugging)
                && nativeDebugging is bool b)
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
                && profile.OtherSettings.TryGetValue(SqlDebuggingProperty, out object sqlDebugging)
                && sqlDebugging is bool b)
            {
                return b;
            }

            return false;
        }
    }
}
