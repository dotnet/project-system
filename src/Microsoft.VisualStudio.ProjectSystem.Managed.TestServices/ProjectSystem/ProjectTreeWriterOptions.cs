// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Flags]
    internal enum ProjectTreeWriterOptions
    {
        None = 0,
        Tags = 1,
        FilePath = 2,
        Flags = 4,
        Visibility = 8,
        Icons = 16,
        ItemType = 32,
        SubType = 64,
        AllProperties = FilePath | Visibility | Flags | Icons | ItemType | SubType
    }
}
