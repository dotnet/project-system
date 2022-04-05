// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class ObservableListExtensions
    {
        public static ObservableList<NameValuePair> CreateList(this IDictionary<string, string> dictionary)
        {
            var list = new ObservableList<NameValuePair>();

            foreach ((string key, string value) in dictionary)
            {
                list.Add(new NameValuePair(key, value, list));
            }

            return list;
        }
    }
}
