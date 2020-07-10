// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
