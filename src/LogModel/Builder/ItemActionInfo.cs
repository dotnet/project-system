// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
