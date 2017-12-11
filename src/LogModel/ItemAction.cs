using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class ItemAction
    {
        public bool IsAddition { get; }
        public ItemGroup ItemGroup { get; }
        public DateTime Time { get; }

        public ItemAction(bool isAddition, ItemGroup itemGroup, DateTime time)
        {
            IsAddition = isAddition;
            ItemGroup = itemGroup;
            Time = time;
        }
    }
}
