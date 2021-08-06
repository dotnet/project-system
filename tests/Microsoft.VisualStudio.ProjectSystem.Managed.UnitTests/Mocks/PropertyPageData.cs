// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class PropertyPageData
    {
        public PropertyPageData(string category, string propertyName, object value, List<object>? setValues = null, Type? propertyType = null)
        {
            Category = category;
            PropertyName = propertyName;
            Value = value;
            PropertyType = propertyType ?? typeof(IStringProperty);
            SetValues = setValues ?? new List<object>();
        }

        public string Category { get; }
        public string PropertyName { get; }
        public object Value { get; }
        public Type PropertyType { get; }

        public List<object> SetValues { get; }
    }
}
