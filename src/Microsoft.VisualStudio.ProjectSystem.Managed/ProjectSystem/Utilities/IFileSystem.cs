// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// An interface wrapper for <see cref="File"/> and <see cref="Directory"/> that gives us the ability to mock
    /// the type for testing.
    /// </summary>
    interface IFileSystem
    {
        FileStream Create(string filePath);

        bool FileExists(string path);
        void RemoveFile(string referenceFile);
        void CopyFile(string source, string destination, bool overwrite);
        string ReadAllText(string filePath);
        void WriteAllText(string filePath, string content);
        void WriteAllText(string filePath, string content, Encoding encoding);
        void WriteAllBytes(string filePath, byte[] bytes);
        DateTime LastFileWriteTime(string filepath);
        DateTime LastFileWriteTimeUtc(string filepath);
        long FileLength(string filename);

        bool DirectoryExists(string dirPath);
        void CreateDirectory(string dirPath);
        void RemoveDirectory(string directoryPath, bool recursive);
        void SetDirectoryAttribute(string directoryPath, FileAttributes newAttribute);
        IEnumerable<string> EnumerateDirectories(string path);
        IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption);
        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

        /// <summary>
        ///     Returns a name suitable for usage as a file or directory name.
        /// </summary>
        /// <returns>
        ///     A <see cref="string"/> containing a name suitable for usage as a file or directory name.
        /// </returns>
        /// <remarks>
        ///     NOTE: Unlike <see cref="Path.GetTempFileName"/>, this method does not create a zero byte file on disk.
        /// </remarks>
        string GetTempDirectoryOrFileName();
    }
}
