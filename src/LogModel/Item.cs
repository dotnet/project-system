using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Item
    {
        public string Name { get; }

        public ImmutableDictionary<string, string> Metadata { get; }

        public Item(string name, ImmutableDictionary<string, string> metadata)
        {
            Name = name;
            Metadata = metadata;
        }
    }
}
