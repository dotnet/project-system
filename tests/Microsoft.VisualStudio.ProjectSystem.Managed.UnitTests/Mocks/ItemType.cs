// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed class ItemType : IItemType
    {
        public string Name { get; }
        public string DisplayName => Name;
        public bool UpToDateCheckInput { get; }

        public ItemType(string name, bool upToDateCheckInput)
        {
            Name = name;
            UpToDateCheckInput = upToDateCheckInput;
        }
    }
}
