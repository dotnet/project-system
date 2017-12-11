using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class ItemActionInfo : BaseInfo
    {
        public bool IsAddition { get; }
        public ItemGroupInfo ItemGroup { get; }
        public DateTime Time { get; }

        public ItemActionInfo(bool isAddition, ItemGroupInfo itemGroup, DateTime time)
        {
            IsAddition = isAddition;
            ItemGroup = itemGroup;
            Time = time;
        }
    }
}
