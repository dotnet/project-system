// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides file and folder operations that operate on the physical <see cref="IProjectTree"/>.
    /// </summary>
    /// <remarks>
    ///     This interface provides a simple facade over the <see cref="IFileSystem"/>, <see cref="IFolderManager"/>,
    ///     <see cref="IProjectTreeService"/>, <see cref="IProjectItemProvider"/> and <see cref="IProjectItemProvider"/> interfaces.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IPhysicalProjectTreeStorage
    {
        /// <summary>
        ///     Adds an existing file to the physical project tree.
        /// </summary>
        /// <param name="path">
        ///     The path of the file to add, can be relative to the project directory.
        /// </param>
        /// <remarks>
        ///     This method will automatically publish the resulting tree to <see cref="IProjectTreeService.CurrentTree"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="path"/> is prefixed with, or contains, only a colon character (:).
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="path"/> contains a colon character (:) that is not part of a drive label ("C:\").
        /// </exception>
        Task AddFileAsync(string path);

        /// <summary>
        ///     Creates a zero-byte file on disk, adding it add to the physical project tree.
        /// </summary>
        /// <param name="path">
        ///     The path of the file to create, can be relative to the project directory.
        /// </param>
        /// <remarks>
        ///     This method will automatically publish the resulting tree to <see cref="IProjectTreeService.CurrentTree"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="path"/> is prefixed with, or contains, only a colon character (:).
        /// </exception>
        /// <exception cref="IOException">
        ///     The file specified by <paramref name="path"/> is a directory.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     The network name is not known.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     The caller does not have the required permission.
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     The specified path is invalid (for example, it is on an unmapped drive).
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="path"/> contains a colon character (:) that is not part of a drive label ("C:\").
        /// </exception>
        Task CreateEmptyFileAsync(string path);

        /// <summary>
        ///     Creates a folder on disk, adding it add to the physical project tree.
        /// </summary>
        /// <param name="path">
        ///     The path of the folder to create, can be relative to the project directory.
        /// </param>
        /// <remarks>
        ///     This method will automatically publish the resulting tree to <see cref="IProjectTreeService.CurrentTree"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="path"/> is prefixed with, or contains, only a colon character (:).
        /// </exception>
        /// <exception cref="IOException">
        ///     The directory specified by <paramref name="path"/> is a file.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     The network name is not known.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     The caller does not have the required permission.
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     The specified path is invalid (for example, it is on an unmapped drive).
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="path"/> contains a colon character (:) that is not part of a drive label ("C:\").
        /// </exception>
        Task CreateFolderAsync(string path);

        /// <summary>
        ///     Adds an existing folder to the physical project tree.
        /// </summary>
        /// <param name="path">
        ///     The path of the folder to add, can be relative to the project directory.
        /// </param>
        /// <remarks>
        ///     This method will automatically publish the resulting tree to <see cref="IProjectTreeService.CurrentTree"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="path"/> is prefixed with, or contains, only a colon character (:).
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="path"/> contains a colon character (:) that is not part of a drive label ("C:\").
        /// </exception>
        Task AddFolderAsync(string path);
    }
}
