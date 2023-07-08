// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Formats and parses the single-string representation of multiple name/value pairs
    /// used for environment variables in launch profiles.
    /// </summary>
    /// <remarks>
    /// We store potentially many environment variables, each having a name and value, in a
    /// single string property value on each launch profile. This class owns the formatting
    /// and parsing of such strings.
    /// </remarks>
    internal static class LaunchProfileEnvironmentVariableEncoding
    {
        public static string Format(ILaunchProfile? profile)
        {
            if (profile is null)
                return "";

            return KeyValuePairListEncoding.Format(profile.EnumerateEnvironmentVariables());
        }

        public static void ParseIntoDictionary(string value, Dictionary<string, string> dictionary)
        {
            dictionary.Clear();

            foreach ((string entryKey, string entryValue) in KeyValuePairListEncoding.Parse(value))
            {
                if (!string.IsNullOrEmpty(entryKey))
                {
                    dictionary[entryKey] = entryValue;
                }
            }
        }
    }
}
