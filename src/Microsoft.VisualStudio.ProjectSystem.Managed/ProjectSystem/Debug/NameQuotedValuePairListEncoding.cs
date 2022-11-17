using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    [Export(typeof(INameValuePairListEncoding))]
    [ExportMetadata("Encoding", "NameQuotedValuePairListEncoding")]
    internal sealed class NameQuotedValuePairListEncoding : INameValuePairListEncoding
    {
        public static KeyQuotedValuePairListEncoding Instance { get; } = new();
        public IEnumerable<(string Name, string Value)> Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            foreach (var entry in ReadEntries(value))
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
        public string Format(IEnumerable<(string Name, string Value)> pairs)
        {
            return string.Join(",", pairs.Select(pair => $"{EncodeCharacters(pair.Name)}=\"{EncodeCharacters(pair.Value)}\""));

            static string EncodeCharacters(string value)
            {
                return value.Replace("/", "//").Replace(",", "/,").Replace("=", "/=");
            }
        }
    }
}
