// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
