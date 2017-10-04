// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal static class LaunchProfileExtensions
    {
        public const string NativeDebuggingProperty = "nativeDebugging";

        /// <summary>
        /// Return a mutable instance
        /// </summary>
        public static IWritableLaunchProfile ToWritableLaunchProfile(this ILaunchProfile curProfile)
        {
            return new WritableLaunchProfile(curProfile);
        }

        public static bool IsInMemoryProfile(this ILaunchProfile profile)
        {
            return profile is ILaunchProfile2 profile2 && profile2.IsInMemoryProfile;
        }

        public static bool IsInMemoryProfile(this IWritableLaunchProfile profile)
        {
            return profile is IWritableLaunchProfile2 profile2 && profile2.IsInMemoryProfile;
        }

        /// <summary>
        /// Returns true if nativeDebugging property is set to true
        /// </summary>
        public static bool NativeDebuggingIsEnabled(this ILaunchProfile profile)
        {
            if (profile.OtherSettings != null 
                && profile.OtherSettings.TryGetValue(NativeDebuggingProperty,  out object nativeDebugging) 
                && nativeDebugging is bool)
            {
                return (bool)nativeDebugging;
            }

            return false;
        }
    }
}
