// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

/// <summary>
///  Encoding corresponding a format of keys eith a respective value that
///  is surrounded in quotes, ex. key1="value1"
/// </summary>
internal sealed class KeyQuotedValuePairListEncoding : StringListEncoding
{
    public static KeyQuotedValuePairListEncoding Instance { get; } = new();

    public override IEnumerable<(string Name, string Value)> Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            yield break;
        }

        input = Decode(input);
        var regex = new Regex(@"(\S+?\s*)\=(\S+?\s*),");

        foreach (Match match in regex.Matches(input))
        {
            string key = match.Groups[1].Value;
            string value = match.Groups[2].Value;

            if (!string.IsNullOrEmpty(key))
            {
                yield return (DecodeCharacters(key), DecodeCharacters(value));
            }
        }

        static string DecodeCharacters(string value)
        {
            return value.Replace("/=", "=").Replace("/,", ",").Replace("//", "/");
        }
    }

    public override string Format(IEnumerable<(string Name, string Value)> pairs)
    {
        return string.Join(",", pairs.Select(pair => $"{EncodeCharacters(pair.Name)}=\"{EncodeCharacters(pair.Value)}\""));

        static string EncodeCharacters(string value)
        {
            return value.Replace("/", "//").Replace(",", "/,").Replace("=", "/=");
        }
    }

    public string Decode(string input)
    {
        input += ",";
        var regex = new Regex(@"(\S+?\s*)\=[\x22](\S+?\s*)[\x22],");
        if (regex.IsMatch(input))
        {
            string decoded = "";
            foreach (Match match in regex.Matches(input))
            {
                decoded += match.Groups[1].Value + "=" + match.Groups[2].Value + ",";
            }
            return decoded;
        }
        else
        {
            return input;
        }
    }
}
