// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    ///  Converts the single string representation to multiple key, value pairs used
    ///  for defining constants.
    /// </summary>
    internal class AbstractProjectConfigurationDefineConstantsEncoding
    {
        private static readonly KeyQuotedValuePairListEncoding _encoding = new();
        private static readonly KeyValuePairListEncoding _dispEncoding = new();

        public static Dictionary<string, string> ParseIntoDictionary(string inputValue)
        {
            Dictionary<string, string> constantsDict = new Dictionary<string, string>();
            constantsDict.Clear();
            foreach ((string key, string value) in _encoding.Parse(inputValue))
            {
                if (!string.IsNullOrEmpty(key))
                    if (!string.IsNullOrEmpty(key))
                    {
                        constantsDict.Add(key, value);
                    }
            }
            return constantsDict;
        }

        public static string Format(Dictionary<string, string> constantsDict)
        {
            return _encoding.Format(EnumerateConstantsDict(constantsDict));
        }

        public static string DisplayFormat(Dictionary<string, string> constantsDict)
        {
            return _dispEncoding.Format(EnumerateConstantsDict(constantsDict));
        }
        private static IEnumerable<(string key, string value)> EnumerateConstantsDict(Dictionary<string, string> constantsDict)
        {
            return constantsDict switch
            {
                null => Enumerable.Empty<(string key, string value)>(),
                { Count: 0 } => Enumerable.Empty<(string key, string value)>(),
                _ => constantsDict.OrderBy(kvp => kvp.Key).Select(kvp => (kvp.Key, kvp.Value))
            };
        }
    }
}
