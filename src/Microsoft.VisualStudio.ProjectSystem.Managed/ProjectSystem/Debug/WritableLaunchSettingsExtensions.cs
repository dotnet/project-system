// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Collections;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal static class WritableLaunchSettingsExtensions
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
                if (!ProfilesAreEqual(launchSettings.Profiles[i], settingsToCompare.Profiles[i]))
                {
                    return true;
                }
            }

            // Check the global settings
            return !DictionaryEqualityComparer<string, object>.Instance.Equals(launchSettings.GlobalSettings, settingsToCompare.GlobalSettings);

            static bool ProfilesAreEqual(IWritableLaunchProfile debugProfile1, IWritableLaunchProfile debugProfile2)
            {
                // Same instance are equal
                if (ReferenceEquals(debugProfile1, debugProfile2))
                {
                    return true;
                }

                if (!string.Equals(debugProfile1.Name, debugProfile2.Name, StringComparisons.LaunchProfileProperties) ||
                   !string.Equals(debugProfile1.CommandName, debugProfile2.CommandName, StringComparisons.LaunchProfileCommandNames) ||
                   !string.Equals(debugProfile1.ExecutablePath, debugProfile2.ExecutablePath, StringComparisons.LaunchProfileProperties) ||
                   !string.Equals(debugProfile1.CommandLineArgs, debugProfile2.CommandLineArgs, StringComparisons.LaunchProfileProperties) ||
                   !string.Equals(debugProfile1.WorkingDirectory, debugProfile2.WorkingDirectory, StringComparisons.LaunchProfileProperties) ||
                   !string.Equals(debugProfile1.LaunchUrl, debugProfile2.LaunchUrl, StringComparisons.LaunchProfileProperties) ||
                   debugProfile1.LaunchBrowser != debugProfile2.LaunchBrowser ||
                   !DictionaryEqualityComparer<string, object>.Instance.Equals(debugProfile1.OtherSettings, debugProfile2.OtherSettings) ||
                   !DictionaryEqualityComparer<string, string>.Instance.Equals(debugProfile1.EnvironmentVariables, debugProfile2.EnvironmentVariables))
                {
                    return false;
                }

                // Compare in-memory states
                return debugProfile1.IsInMemoryObject() == debugProfile2.IsInMemoryObject();
            }
        }
    }
}
