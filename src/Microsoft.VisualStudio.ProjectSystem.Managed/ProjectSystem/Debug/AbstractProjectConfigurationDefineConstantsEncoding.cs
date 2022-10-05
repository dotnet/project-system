// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

/// <summary>
///  Converts the single string representation to multiple key, value pairs used
///  for defining constants.
/// </summary>
internal abstract class AbstractProjectConfigurationDefineConstantsEncoding
{
    public static Dictionary<string, string> ParseIntoDictionary(string inputValue)
    {
        Dictionary<string, string> constantsDictionary = new Dictionary<string, string>();
        foreach ((string key, string value) in KeyQuotedValuePairListEncoding.Instance.Parse(inputValue))
        {
            if (!string.IsNullOrEmpty(key))
            {
                constantsDictionary[key] = value;
            }
        }
        return constantsDictionary;
    }

    public static string Format(Dictionary<string, string> constantsDictionary)
    {
        return KeyQuotedValuePairListEncoding.Instance.Format(EnumerateConstantsDictionary(constantsDictionary));
    }

    public static string DisplayFormat(Dictionary<string, string> constantsDictionary)
    {
        return KeyValuePairListEncoding.Instance.Format(EnumerateConstantsDictionary(constantsDictionary));
    }

    private static IEnumerable<(string key, string value)> EnumerateConstantsDictionary(Dictionary<string, string> constantsDictionary)
    {
        return constantsDictionary switch
        {
            null or  { Count: 0 } => Enumerable.Empty<(string key, string value)>(),
            _ => constantsDictionary.OrderBy(kvp => kvp.Key).Select(kvp => (kvp.Key, kvp.Value))
        };
    }
}
