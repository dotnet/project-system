// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.Designer;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    [Export(typeof(INameValuePairListEncoding))]
    [ExportMetadata("Encoding", "VBDefineConstantsEncoding")]
    internal sealed class VBDefineConstantsEncoding : INameValuePairListEncoding
    {
        public IEnumerable<(string Name, string Value)> Parse(string value)
        {
            Requires.NotNull(value);
            if (string.IsNullOrEmpty(value))
            {
                yield break;
            }
            foreach (var entry in ReadEntries(value))
            {
                (string, string) resultingEntry = ("", "");
                try
                {
                    (string entryKey, string entryValue) = SplitPairedEntry(entry);
                    resultingEntry = (DecodeCharacters(entryKey), DecodeCharacters(entryValue));
                }
                // if not key value pair entry, tries to parse for single value entry
                // empty string in the section position signifies single value entry
                catch (FormatException)
                {
                    resultingEntry = (DecodeCharacters(SplitSingleEntry(entry)), "");
                }
                yield return resultingEntry;
                
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

            static (string EncodedKey, string EncodedValue) SplitPairedEntry(string entry)
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
                            throw new FormatException($"Expected valid name value pair for defining custom constants.");
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
                throw new FormatException("Expected valid name value pair for defining custom constants.");
            }

            static string SplitSingleEntry(string entry)
            {
                bool escaped = false;
                for (int i = 0; i < entry.Length; i++)
                {
                    if (entry[i] == '=' && !escaped)
                    {
                        throw new FormatException($"Expected valid name value pair for defining custom constants.");
                    }
                    else if (i == entry.Length - 1)
                    {
                        return entry;
                    }
                    escaped = entry[i] == '/' && !escaped;
                }
                throw new FormatException("Expected valid name value pair for defining custom constants.");
            }

            static string DecodeCharacters(string value)
            {
                return value.Replace("/\"", "\"").Replace("/=", "=").Replace("/,", ",").Replace("//", "/");
            }
        }

        public string Format(IEnumerable<(string Name, string Value)> pairs)
        {
            return string.Join(",", pairs.Select(EncodePair));

            static string EncodePair((string Name, string Value) pair)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    return EncodeCharacters(pair.Name);
                }
                else
                {
                    return $"{EncodeCharacters(pair.Name)}={EncodeCharacters(pair.Value)}";
                }
            }
            
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
