// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

/// <summary>
///  Encoding corresponding a format of keys eith a respective value that
///  is surrounded in quotes, ex. key1="value1"
/// </summary>
internal sealed class KeyQuotedValuePairListEncoding
{
    public static KeyQuotedValuePairListEncoding Instance { get; } = new();

    public IEnumerable<(string Name, string Value)> Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            yield break;
        }

        input = input.Replace("\"", "") + ",";
        var regex = new Regex(@"(\S+?\s*)\=(\S+?\s*),");

        foreach (Match match in regex.Matches(input))
        {
            string key = match.Groups[1].Value;
            string value = match.Groups[2].Value;

            if (!string.IsNullOrEmpty(key))
            {
                yield return (Decode(key), Decode(value));
            }
        }

        static string Decode(string value)
        {
            return value.Replace("/=", "=").Replace("/,", ",").Replace("//", "/");
        }
    }

    public string Format(IEnumerable<(string Name, string Value)> pairs)
    {
        return string.Join(",", pairs.Select(pair => $"{Encode(pair.Name)}=\"{Encode(pair.Value)}\""));

        static string Encode(string value)
        {
            return value.Replace("/", "//").Replace(",", "/,").Replace("=", "/=");
        }
    }
}
