// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal static class ILaunchProfileExtensions
    {
        /// <summary>
        /// Retrieves a dictionary of the environment variables in the profile.
        /// </summary>
        /// <remarks>
        /// While the profile already has a <see cref="ILaunchProfile.EnvironmentVariables"/> property,
        /// it returns an <see cref="ImmutableDictionary{TKey, TValue}"/> which does not preserve order.
        /// This extension method looks for internal state on <paramref name="profile"/> that allows
        /// the returned <see cref="Dictionary{TKey, TValue}"/> to have its items in the correct order
        /// for this data, as specified in the <c>launchSettings.json</c> file.
        /// </remarks>
        /// <param name="profile">The profile to read from.</param>
        /// <returns>An ordered map of environment variable names to values, or <see langword="null"/> if none were specified.</returns>
        public static Dictionary<string, string>? GetEnvironmentVariablesDictionary(this ILaunchProfile profile)
        {
            return profile switch
            {
                { EnvironmentVariables: null or { Count: 0 } } => null,
                LaunchProfile launchProfile => ToDictionary(profile.EnvironmentVariables, launchProfile.EnvironmentVariablesKeyOrder, StringComparers.EnvironmentVariableNames),
                _ => profile.EnvironmentVariables.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparers.EnvironmentVariableNames)
            };
        }

        /// <summary>
        /// Enumerates the profile's environment variables, preserving order if possible.
        /// </summary>
        /// <remarks>
        /// While the profile already has a <see cref="ILaunchProfile.EnvironmentVariables"/> property,
        /// it returns an <see cref="ImmutableDictionary{TKey, TValue}"/> which does not preserve order.
        /// This extension method looks for internal state on <paramref name="profile"/> that allows
        /// enumeration of environment variables in the same order they are specified in the
        /// <c>launchSettings.json</c> file.
        /// </remarks>
        /// <param name="profile">The profile to read from.</param>
        /// <returns>An ordered enumeration of environment variable name/value pairs.</returns>
        public static IEnumerable<KeyValuePair<string, string>> EnumerateEnvironmentVariables(this ILaunchProfile profile)
        {
            return profile switch
            {
                { EnvironmentVariables: null or { Count: 0 } } => Enumerable.Empty<KeyValuePair<string, string>>(),
                LaunchProfile launchProfile => Enumerate(launchProfile),
                _ => profile.EnvironmentVariables.OrderBy(pair => pair.Key, StringComparers.EnvironmentVariableNames)
            };

            static IEnumerable<KeyValuePair<string, string>> Enumerate(LaunchProfile profile)
            {
                foreach (string key in profile.EnvironmentVariablesKeyOrder)
                    yield return new(key, profile.EnvironmentVariables![key]);
            }
        }

        public static Dictionary<string, object>? GetOtherSettingsDictionary(this ILaunchProfile profile)
        {
            return profile switch
            {
                { OtherSettings: null or { Count: 0 } } => null,
                LaunchProfile launchProfile => ToDictionary(profile.OtherSettings, launchProfile.OtherSettingsKeyOrder, StringComparers.LaunchProfileProperties),
                _ => profile.OtherSettings.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparers.LaunchProfileProperties)
            };
        }

        private static Dictionary<string, T> ToDictionary<T>(ImmutableDictionary<string, T> dic, ImmutableArray<string> keyOrder, IEqualityComparer<string> comparer)
        {
            var result = new Dictionary<string, T>(comparer);

            foreach (string key in keyOrder)
            {
                result.Add(key, dic[key]);
            }

            return result;
        }
    }
}
