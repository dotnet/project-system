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
