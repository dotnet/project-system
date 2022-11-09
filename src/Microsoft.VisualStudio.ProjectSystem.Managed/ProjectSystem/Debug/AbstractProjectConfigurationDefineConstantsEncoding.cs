// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

/// <summary>
///  Converts the single string representation to multiple key, value pairs used
///  for defining constants.
/// </summary>
internal abstract class StringListEncoding
{
    public abstract IEnumerable<(string Name, string Value)> Parse(string input);
    public abstract string Format(IEnumerable<(string Name, string Value)> pairs);

    public static Dictionary<string, string> ParseIntoDictionary(string inputValue)
    {
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        foreach ((string key, string value) in KeyQuotedValuePairListEncoding.Instance.Parse(inputValue))
        {
            if (!string.IsNullOrEmpty(key))
            {
                dictionary[key] = value;
            }
        }
        return dictionary;
    }

    public static IEnumerable<(string Name, string Value)> EnumerateDictionary(Dictionary<string, string> dictionary)
    {

        return dictionary switch
        {
            null or { Count: 0 } => Enumerable.Empty<(string key, string value)>(),
            _ => dictionary.ToList().Select(kvp => (kvp.Key, kvp.Value))
        };
    }
}
