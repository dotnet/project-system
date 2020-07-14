// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
