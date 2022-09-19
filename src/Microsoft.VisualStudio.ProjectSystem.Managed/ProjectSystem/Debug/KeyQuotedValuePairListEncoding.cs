// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal sealed class KeyQuotedValuePairListEncoding
    {
        public IEnumerable<(string Name, string Value)> Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                yield break;
            }

            foreach (var entry in ReadEntries(input))
            {
                var (entryKey, entryValue) = SplitEntry(entry);
                var decodedEntryKey = Decode(entryKey);
                var decodedEntryValue = Decode(entryValue);

                if (!string.IsNullOrEmpty(decodedEntryKey))
                {
                    yield return (decodedEntryKey, decodedEntryValue);
                }
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
                        // if (entry[i + 1] == '"' && entry[entry.Length - 1] == '"')
                        // {
                        //    return (entry.Substring(0, i), entry.Substring(i + 2, entry.Length - 1));
                        //}
                        //else
                        //{
                        //    return (entry.Substring(0, i), entry.Substring(i + 1));
                        //}
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

            static string Decode(string value)
            {
                return value.Replace("/=", "=").Replace("/,", ",").Replace("//", "/");
            }
        }

        //public string Format( IEnumerable<(string Name, string Value)> pairs)
        //{
         //   return string.Join(",", pairs.Select(pair => $"{Encode(pair.Name)}=\"{Encode(pair.Value)}\""));

          //  static string Encode(string value)
           // {
          //      return value.Replace("/", "//").Replace(",", "/,").Replace("=", "/=");
         //   }
        //}
    }
}
