// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

/// <summary>
///  Converts the single string representation to multiple key, value pairs used
///  for defining constants.
/// </summary>
internal abstract class AbstractProjectConfigurationDefineConstantsEncoding
{
    private static Dictionary<string, string> ParseIntoDictionary(string inputValue)
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

    public static string Format(string propertyValue)
    {
        Dictionary<string, string> constantsDictionary = ParseIntoDictionary(propertyValue);
        return KeyQuotedValuePairListEncoding.Instance.Format(EnumerateConstantsDictionary(constantsDictionary));
    }

    public static string DisplayFormat(string propertyValue)
    {
        Dictionary<string, string> constantsDictionary = ParseIntoDictionary(propertyValue);
        return KeyValuePairListEncoding.Instance.Format(EnumerateConstantsDictionary(constantsDictionary));
    }

    private static IEnumerable<(string Name, string Value)> EnumerateConstantsDictionary(Dictionary<string, string> constantsDictionary)
    {

        return constantsDictionary switch
        {
            null or  { Count: 0 } => Enumerable.Empty<(string key, string value)>(),
            _ => constantsDictionary.ToList().Select(kvp => (kvp.Key, kvp.Value))
        };
    }
}
