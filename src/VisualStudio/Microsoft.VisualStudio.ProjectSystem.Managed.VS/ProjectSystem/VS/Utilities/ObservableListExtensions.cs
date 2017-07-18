// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class ObservableListExtensions
    {
        public static ObservableList<NameValuePair> CreateList(this IDictionary<string, string> dictionary)
        {
            ObservableList<NameValuePair> list = new ObservableList<NameValuePair>();
            foreach (var kvp in dictionary)
            {
                list.Add(new NameValuePair(kvp.Key, kvp.Value, list));
            }
            return list;
        }

        public static IDictionary<string, string> CreateDictionary(this ObservableList<NameValuePair> list)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var ev in list)
            {
                dictionary.Add(ev.Name, ev.Value);
            }
            return dictionary;
        }
    }
}
