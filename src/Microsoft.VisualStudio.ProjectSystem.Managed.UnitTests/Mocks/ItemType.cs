// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
