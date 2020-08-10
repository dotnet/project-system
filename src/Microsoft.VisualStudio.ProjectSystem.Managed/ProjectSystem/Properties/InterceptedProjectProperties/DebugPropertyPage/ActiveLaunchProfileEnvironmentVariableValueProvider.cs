// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Reads and writes the <see cref="ILaunchProfile.EnvironmentVariables"/> property
    /// of the active <see cref="ILaunchProfile"/> via the <see cref="ILaunchSettingsProvider"/>.
    /// </summary>
    /// <remarks>
    /// Most of the properties of the <see cref="ILaunchProfile"/> are handled by <see cref="ActiveLaunchProfileCommonValueProvider"/>
    /// and <see cref="ActiveLaunchProfileExtensionValueProvider"/>. Handling of <see cref="ILaunchProfile.EnvironmentVariables"/>
    /// is complex enough to warrant its own value provider.
    /// </remarks>
    [ExportInterceptingPropertyValueProvider(EnvironmentVariablesPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class ActiveLaunchProfileEnvironmentVariableValueProvider : LaunchSettingsValueProviderBase
    {
        internal const string EnvironmentVariablesPropertyName = "EnvironmentVariables";

        [ImportingConstructor]
        public ActiveLaunchProfileEnvironmentVariableValueProvider(ILaunchSettingsProvider launchSettingsProvider)
            : base(launchSettingsProvider)
        {
        }

        public override string? GetPropertyValue(string propertyName, ILaunchSettings launchSettings)
        {
            if (propertyName != EnvironmentVariablesPropertyName)
            {
                throw new InvalidOperationException($"{nameof(ActiveLaunchProfileEnvironmentVariableValueProvider)} does not handle property '{propertyName}'.");
            }

            return ConvertDictionaryToString(launchSettings.ActiveProfile?.EnvironmentVariables);
        }

        public override bool SetPropertyValue(string propertyName, string value, IWritableLaunchSettings launchSettings)
        {
            if (propertyName != EnvironmentVariablesPropertyName)
            {
                throw new InvalidOperationException($"{nameof(ActiveLaunchProfileEnvironmentVariableValueProvider)} does not handle property '{propertyName}'.");
            }

            var activeProfile = launchSettings.ActiveProfile;
            if (activeProfile == null)
            {
                return false;
            }

            ParseStringIntoDictionary(value, activeProfile.EnvironmentVariables);

            return true;
        }

        private static string ConvertDictionaryToString(ImmutableDictionary<string, string>? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return string.Join(",", value.OrderBy(kvp => kvp.Key, StringComparer.Ordinal).Select(kvp => $"{encode(kvp.Key)}={encode(kvp.Value)}"));

            static string encode(string value)
            {
                return value.Replace("/", "//").Replace(",", "/,").Replace("=", "/=");
            }
        }
        private static void ParseStringIntoDictionary(string value, Dictionary<string, string> dictionary)
        {
            dictionary.Clear();

            foreach (var entry in readEntries(value))
            {
                var (entryKey, entryValue) = splitEntry(entry);
                var decodedEntryKey = decode(entryKey);
                var decodedEntryValue = decode(entryValue);

                if (!string.IsNullOrEmpty(decodedEntryKey))
                {
                    dictionary[decodedEntryKey] = decodedEntryValue;
                }
            }

            static IEnumerable<string> readEntries(string rawText)
            {
                bool escaped = false;
                int entryStart = 0;
                for (int i = 0; i < rawText.Length; i++)
                {
                    if (rawText[i] == ',' && !escaped)
                    {
                        yield return rawText.Substring(entryStart, i - entryStart);
                        entryStart = i + 1;
                        escaped = false;
                    }
                    else if (rawText[i] == '/')
                    {
                        escaped = !escaped;
                    }
                    else
                    {
                        escaped = false;
                    }
                }

                yield return rawText.Substring(entryStart);
            }

            static (string encodedKey, string encodedValue) splitEntry(string entry)
            {
                bool escaped = false;
                for (int i = 0; i < entry.Length; i++)
                {
                    if (entry[i] == '=' && !escaped)
                    {
                        return (entry.Substring(0, i), entry.Substring(i + 1));
                    }
                    else if (entry[i] == '/')
                    {
                        escaped = !escaped;
                    }
                    else
                    {
                        escaped = false;
                    }
                }

                return (string.Empty, string.Empty);
            }

            static string decode(string value)
            {
                return value.Replace("/=", "=").Replace("/,", ",").Replace("//", "/");
            }
        }
    }
}
