// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Collections;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal static class WritableLaunchSettingsExtension
    {
        public static bool SettingsDiffer(this IWritableLaunchSettings launchSettings, IWritableLaunchSettings settingsToCompare)
        {
            if (launchSettings.Profiles.Count != settingsToCompare.Profiles.Count)
            {
                return true;
            }

            // Now compare each item. We can compare in order. If the lists are different then the settings are different even
            // if they contain the same items
            for (int i = 0; i < launchSettings.Profiles.Count; i++)
            {
                if (!WritableLaunchProfile.ProfilesAreEqual(launchSettings.Profiles[i], settingsToCompare.Profiles[i]))
                {
                    return true;
                }
            }

            // Check the global settings
            return !DictionaryEqualityComparer<string, object>.Instance.Equals(
                launchSettings.GlobalSettings.ToImmutableDictionary(),
                settingsToCompare.GlobalSettings.ToImmutableDictionary());
        }
    }
}
