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
