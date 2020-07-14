// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class PropertySet
    {
        public string Name { get; }
        public string Value { get; }
        public DateTime Time { get; }

        public PropertySet(string name, string value, DateTime time)
        {
            Name = name;
            Value = value;
            Time = time;
        }
    }
}
