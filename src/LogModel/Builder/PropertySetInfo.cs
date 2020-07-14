// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class PropertySetInfo : BaseInfo
    {
        public string Name { get; }
        public string Value { get; }
        public DateTime Time { get; }

        public PropertySetInfo(string name, string value, DateTime time)
        {
            Name = name;
            Value = value;
            Time = time;
        }
    }
}
