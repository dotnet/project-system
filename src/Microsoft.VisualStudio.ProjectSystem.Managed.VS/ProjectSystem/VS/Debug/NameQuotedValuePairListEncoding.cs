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
            Requires.NotNull(value, nameof(value));
            if (string.IsNullOrEmpty(value))
            {
                yield break;
            }
            foreach (var entry in ReadEntries(value))
            {
                var (entryKey, entryValue) = SplitEntry(entry);
                yield return (DecodeCharacters(entryKey), StripQuotes(DecodeCharacters(entryValue)));
            }

            static IEnumerable<string> ReadEntries(string rawText)
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

            static (string EncodedKey, string EncodedValue) SplitEntry(string entry)
            {
                bool escaped = false;
                for (int i = 0; i < entry.Length; i++)
                {
                    if (entry[i] == '=' && !escaped)
                    {
                        var name = entry.Substring(0, i);
                        var value = entry.Substring(i + 1);
                        if (name.Length == 0 || value.Length == 0)
                        {
                            throw new FormatException($"Expected valid name value pair.");
                        }
                        return (name, value);
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
                throw new FormatException("Expected valid name value pair.");
            }

            static string DecodeCharacters(string value)
            {

                return value.Replace("/\"", "\"").Replace("/=", "=").Replace("/,", ",").Replace("//", "/");
            }

            static string StripQuotes(string value)
            {
                if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                {
                    return value.Substring(1, value.Length - 2);
                }
                return value;
            }
        }

        public string Format(IEnumerable<(string Name, string Value)> pairs)
        {
            return string.Join(",", pairs.Select(pair => $"{EncodeCharacters(pair.Name)}=\"{EncodeCharacters(pair.Value)}\""));

            static string EncodeCharacters(string value)
            {
                int i = value.Length - 1;
                while (i > -1)
                {
                    if (value[i] == '/' || value[i] == ',' || value[i] == '=' || value[i] == '"')
                    {
                        if (i == 0 || value[i - 1] != '/')
                        {
                            value = value.Insert(i, "/");
                        }
                        else if (value[i - 1] == '/')
                        {
                            i--;
                        }
                    }
                    i--;
                }
                return value;
            }
        }
    }
}
