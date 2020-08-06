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
    /// <para>
    /// Reads and writes the common properties of the active <see cref="ILaunchProfile"/>
    /// via the <see cref="ILaunchSettingsProvider"/>.
    /// </para>
    /// <para>
    /// "Common" here means properties that are stored in the named properties of <see cref="ILaunchProfile"/>,
    /// rather than the <see cref="ILaunchProfile.OtherSettings"/> dictionary. Those are
    /// handled by the <see cref="ActiveLaunchProfileExtensionValueProvider"/>.
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
            CommandLineArgumentsPropertyName,
            EnvironmentVariablesPropertyName,
            ExecutablePathPropertyName,
            LaunchBrowserPropertyName,
            LaunchTargetPropertyName,
            LaunchUrlPropertyName,
            WorkingDirectoryPropertyName,
        },
        ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class ActiveLaunchProfileCommonValueProvider : LaunchSettingsValueProviderBase
    {
        internal const string CommandLineArgumentsPropertyName = "CommandLineArguments";
        internal const string EnvironmentVariablesPropertyName = "EnvironmentVariables";
        internal const string ExecutablePathPropertyName = "ExecutablePath";
        internal const string LaunchBrowserPropertyName = "LaunchBrowser";
        internal const string LaunchTargetPropertyName = "LaunchTarget";
        internal const string LaunchUrlPropertyName = "LaunchUrl";
        internal const string WorkingDirectoryPropertyName = "WorkingDirectory";

        [ImportingConstructor]
        public ActiveLaunchProfileCommonValueProvider(UnconfiguredProject project, ILaunchSettingsProvider launchSettingsProvider, IProjectThreadingService projectThreadingService)
            : base(project, launchSettingsProvider, projectThreadingService)
        {
        }

        public override string? GetPropertyValue(string propertyName, ILaunchSettings launchSettings)
        {
            string? activeProfilePropertyValue = propertyName switch
            {
                CommandLineArgumentsPropertyName => launchSettings.ActiveProfile?.CommandLineArgs,
                EnvironmentVariablesPropertyName => ConvertDictionaryToString(launchSettings.ActiveProfile?.EnvironmentVariables),
                ExecutablePathPropertyName => launchSettings.ActiveProfile?.ExecutablePath,
                LaunchBrowserPropertyName => ConvertBooleanToString(launchSettings.ActiveProfile?.LaunchBrowser),
                LaunchTargetPropertyName => launchSettings.ActiveProfile?.CommandName,
                LaunchUrlPropertyName => launchSettings.ActiveProfile?.LaunchUrl,
                WorkingDirectoryPropertyName => launchSettings.ActiveProfile?.WorkingDirectory,
                _ => throw new InvalidOperationException($"{nameof(ActiveLaunchProfileCommonValueProvider)} does not handle property '{propertyName}'.")
            };

            return activeProfilePropertyValue;
        }

        public override bool SetPropertyValue(string propertyName, string value, IWritableLaunchSettings launchSettings)
        {
            var activeProfile = launchSettings.ActiveProfile;
            if (activeProfile == null)
            {
                return false;
            }

            UpdateActiveLaunchProfile(activeProfile, propertyName, value);

            return true;
        }

        private static string? ConvertBooleanToString(bool? value)
        {
            if (value == null)
            {
                return null;
            }
            else if (value.Value)
            {
                return "true";
            }
            else
            {
                return "false";
            }
        }

        private static string ConvertDictionaryToString(ImmutableDictionary<string, string>? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return string.Join(",", value.Select(kvp => $"{encode(kvp.Key)}={encode(kvp.Value)}"));

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

        private static void UpdateActiveLaunchProfile(IWritableLaunchProfile activeProfile, string propertyName, string newValue)
        {
            switch (propertyName)
            {
                case CommandLineArgumentsPropertyName:
                    activeProfile.CommandLineArgs = newValue;
                    break;

                case ExecutablePathPropertyName:
                    activeProfile.ExecutablePath = newValue;
                    break;

                case EnvironmentVariablesPropertyName:
                    ParseStringIntoDictionary(newValue, activeProfile.EnvironmentVariables);
                    break;

                case LaunchBrowserPropertyName:
                    activeProfile.LaunchBrowser = bool.Parse(newValue);
                    break;

                case LaunchTargetPropertyName:
                    activeProfile.CommandName = newValue;
                    break;

                case LaunchUrlPropertyName:
                    activeProfile.LaunchUrl = newValue;
                    break;

                case WorkingDirectoryPropertyName:
                    activeProfile.WorkingDirectory = newValue;
                    break;

                default:
                    throw new InvalidOperationException($"{nameof(ActiveLaunchProfileCommonValueProvider)} does not handle property '{propertyName}'.");
            }
        }
    }
}
