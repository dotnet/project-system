// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// A wrapper for <see cref="File"/> that gives us the ability to mock
    /// the type for testing.
    /// </summary>
    [Export(typeof(IFileSystem))]
    internal class FileSystem : IFileSystem
    {
        public FileStream Create(string filePath)
        {
            return File.Create(filePath);
        }

        public bool Exists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}
