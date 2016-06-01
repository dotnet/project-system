// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// An interface wrapper for <see cref="File"/> that gives us the ability to mock
    /// the type for testing.
    /// </summary>
    interface IFileSystem
    {
        bool Exists(string filePath);
        FileStream Create(string filePath);
    }
}
