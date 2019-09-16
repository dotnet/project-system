// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.IO
{
    /// <summary>
    ///     Provides static methods for the creation, copying, deletion, moving of files and directories.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IFileSystem
    {
        Stream Create(string path);

        bool FileExists(string path);
        void RemoveFile(string path);
        void CopyFile(string source, string destination, bool overwrite);
        string ReadAllText(string path);
        void WriteAllText(string path, string content);
        void WriteAllText(string path, string content, Encoding encoding);
        void WriteAllBytes(string path, byte[] bytes);
        DateTime LastFileWriteTime(string path);
        DateTime LastFileWriteTimeUtc(string path);
        long FileLength(string filename);

        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        void RemoveDirectory(string path, bool recursive);
        void SetDirectoryAttribute(string path, FileAttributes newAttribute);
        string GetCurrentDirectory();
        void SetCurrentDirectory(string directory);
        string GetFullPath(string path);

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
