// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    ///  Converts the single string representation og multiple key, value pairs into a dictionary
    ///  for defining constants.
    /// </summary>
    /// <remarks>
    /// There are many constants that could be set, implying the dictionary key, value format.
    /// </remarks>
    internal static class AbstractProjectConfigurationDefineConstants
    {
        private static readonly KeyQuotedValuePairListEncoding _encoding = new();

        public static Dictionary<string, string> ParseIntoDictionary(string inputValue)
        {
            Dictionary<string, string> constantsDict = new Dictionary<string, string>();
            constantsDict.Clear();
            foreach ((string key, string value) in _encoding.Parse(inputValue))
            {
                if (!string.IsNullOrEmpty(key))
                {
                    constantsDict.Add(key, value);
                }
            }
            return constantsDict;
        }
    }
}
