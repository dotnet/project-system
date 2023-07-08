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

            foreach (string entry in ReadEntries(value))
            {
                if (!TryParseSplitPairedEntry(entry, out (string, string) resultingEntry)
                    && !TryParseSingleEntry(entry, out resultingEntry))
                {
                    throw new FormatException("Expected valid name value pair for defining custom constants.");
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

            static bool TryParseSplitPairedEntry(string entry, out (string, string) result)
            {
                bool escaped = false;
                for (int i = 0; i < entry.Length; i++)
                {
                    if (entry[i] == '=' && !escaped)
                    {
                        string name = entry.Substring(0, i);
                        string value = entry.Substring(i + 1);
                        if (name.Length == 0 || value.Length == 0)
                        {
                            break;
                        }
                        result = (DecodeCharacters(name), DecodeCharacters(value));

                        return true;
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

                result = default;
                return false;
            }

            static bool TryParseSingleEntry(string entry, out (string, string) result)
            {
                bool escaped = false;
                for (int i = 0; i < entry.Length; i++)
                {
                    if (entry[i] == '=' && !escaped)
                    {
                        break;
                    }
                    else if (i == entry.Length - 1)
                    {
                        result = (DecodeCharacters(entry), "");
                        return true;
                    }
                    escaped = entry[i] == '/' && !escaped;
                }

                result = default;
                return false;
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
