// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.Designer;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    [Export(typeof(INameValuePairListEncoding))]
    [ExportMetadata("Encoding", "NameQuotedValuePairListEncoding")]
    internal sealed class NameQuotedValuePairListEncoding : INameValuePairListEncoding
    {
        public IEnumerable<(string Name, string Value)> Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            foreach (var entry in ReadEntries(value))
            {
                var (entryKey, entryValue) = SplitEntry(entry);

                if (!string.IsNullOrEmpty(entryKey))
                {
                    yield return (entryKey, entryValue);
                }
            }
        }

        public string Format(IEnumerable<(string Name, string Value)> pairs)
        {
            return string.Join(",", pairs.Select(pair => $"{EncodeCharacters(pair.Name)}=\"{EncodeCharacters(pair.Value)}\""));

            static string EncodeCharacters(string value)
            {
                return value.Replace("/", "//").Replace(",", "/,").Replace("=", "/=");
            }
        }

        public IEnumerable<(string Name, string Value)> Decode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            foreach (var entry in ReadEntries(value))
            {
                var (entryKey, entryQuotedValue) = SplitEntry(entry);
                var decodedEntryKey = DecodeCharacters(entryKey);
                var decodedEntryValue = DecodeCharacters(Unquote(entryQuotedValue));

                if (!string.IsNullOrEmpty(decodedEntryKey))
                {
                    yield return (decodedEntryKey, decodedEntryValue);
                }
            }

            static string Unquote(string value)
            {   if (value.Length > 2)
                {
                    return value.Substring(1, value.Length - 2);
                }
                else
                {
                    return string.Empty;
                }
            }

            static string DecodeCharacters(string value)
            {
                return value.Replace("/=", "=").Replace("/,", ",").Replace("//", "/");
            }
        }

        public string DisplayFormat(IEnumerable<(string Name, string Value)> pairs)
        {
            return string.Join(",", pairs.Select(pair => $"{pair.Name}={pair.Value}"));
        }

        private static IEnumerable<string> ReadEntries(string rawText)
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

        private static (string EncodedKey, string EncodedValue) SplitEntry(string entry)
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
    }
}
