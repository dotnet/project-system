// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        bool PathExists(string path);

        bool FileExists(string path);
        void RemoveFile(string path);
        void CopyFile(string source, string destination, bool overwrite);
        string ReadAllText(string path);
        void WriteAllText(string path, string content);
        void WriteAllText(string path, string content, Encoding encoding);
        void WriteAllBytes(string path, byte[] bytes);

        /// <summary>
        ///     Return the date and time, in coordinated universal time (UTC), that the specified file or directory was last written to,
        ///     or <see cref="DateTime.MinValue"/> if the path does not exist or is not accessible.
        /// </summary>
        DateTime GetLastFileWriteTimeOrMinValueUtc(string path);

        /// <summary>
        ///     Return the date and time, in coordinated universal time (UTC), that the specified file or directory was last written to,
        ///     indicating if the path exists and is accessible.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="path"/> exists and is accessible; otherwise, <see langword="false"/>.
        /// </returns>
        bool TryGetLastFileWriteTimeUtc(string path, [NotNullWhen(true)]out DateTime? result);

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
