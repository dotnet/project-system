// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

internal static class KeyValuePairListEncoding
{
    /// <summary>
    /// Parses the input string into a collection of key-value pairs with the given separator.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="allowsEmptyKey">Indicates whether empty keys are allowed. If this is true, a pair will be returned if an empty key has a non-empty value. ie, =4</param>
    /// <param name="separator">The character used to separate entries in the input string.</param>
    public static IEnumerable<(string Name, string Value)> Parse(string input, char separator = ',')
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            yield break;
        }

        foreach (var entry in ReadEntries(input, separator))
        {
            var (entryKey, entryValue) = SplitEntry(entry);
            var decodedEntryKey = Decode(entryKey);
            var decodedEntryValue = Decode(entryValue);
            
            if (!string.IsNullOrEmpty(decodedEntryKey))
            {
                yield return (decodedEntryKey, decodedEntryValue);
            }
        }

        static IEnumerable<string> ReadEntries(string rawText, char separator)
        {
            bool escaped = false;
            int entryStart = 0;
            for (int i = 0; i < rawText.Length; i++)
            {
                if (rawText[i] == separator && !escaped)
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

        static (string EncodedKey, string EncodedValue) SplitEntry(string entry)
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

            return (entry, string.Empty);
        }

        static string Decode(string value)
        {
            return value.Replace("/=", "=").Replace("/,", ",").Replace("//", "/");
        }
    }

    public static string Format(IEnumerable<(string Name, string Value)> pairs, char separator = ',')
    {
        return string.Join(
            separator.ToString(),
            pairs.Select(kvp => string.IsNullOrEmpty(kvp.Value) 
                ? Encode(kvp.Name) 
                : $"{Encode(kvp.Name)}={Encode(kvp.Value)}"));

        static string Encode(string value)
        {
            return value.Replace("/", "//").Replace(",", "/,").Replace("=", "/=");
        }
    }
}
