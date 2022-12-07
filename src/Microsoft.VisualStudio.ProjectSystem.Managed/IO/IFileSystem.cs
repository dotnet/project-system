// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
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
        /// <summary>
        /// Creates or overwrites an empty file with the specified path.
        /// </summary>
        void Create(string path);

        /// <summary>
        /// Gets whether <paramref name="path"/> is a file or directory that exists on disk.
        /// </summary>
        bool PathExists(string path);

        /// <summary>
        /// Gets whether <paramref name="path"/> is a file that exists on disk.
        /// </summary>
        bool FileExists(string path);

        void RemoveFile(string path);
        void CopyFile(string source, string destination, bool overwrite);
        Task<string> ReadAllTextAsync(string path);
        Stream OpenTextStream(string path);
        Task WriteAllTextAsync(string path, string content);

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
        bool TryGetLastFileWriteTimeUtc(string path, [NotNullWhen(true)] out DateTime? result);

        bool TryGetFileSizeBytes(string path, out long result);

        (long SizeBytes, DateTime WriteTimeUtc)? GetFileSizeAndWriteTimeUtc(string path);

        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        string GetFullPath(string path);
    }
}
