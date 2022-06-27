// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;

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

            var sb = new StringBuilder();

            foreach ((string key, string value) in profile.EnumerateEnvironmentVariables())
            {
                if (sb.Length != 0)
                    sb.Append(',');
                sb.Append(encode(key));
                sb.Append('=');
                sb.Append(encode(value));
            }

            return sb.ToString();

            static string encode(string value)
            {
                return value.Replace("/", "//").Replace(",", "/,").Replace("=", "/=");
            }
        }

        public static void ParseIntoDictionary(string value, Dictionary<string, string> dictionary)
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
