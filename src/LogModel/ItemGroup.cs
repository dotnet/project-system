using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class ItemGroup
    {
        public string Name { get; }
        public ImmutableList<Item> Items { get; }

        public ItemGroup(string name, ImmutableList<Item> items)
        {
            Name = name;
            Items = items;
        }
    }
}
