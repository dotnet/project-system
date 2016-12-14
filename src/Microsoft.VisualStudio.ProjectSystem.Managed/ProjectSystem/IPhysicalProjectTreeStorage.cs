// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides file and folder operations that operate on the physical <see cref="IProjectTree"/>.
    /// </summary>
    /// <remarks>
    ///     This interface provides a simple facade over the <see cref="IFileSystem"/>, <see cref="IFolderManager"/>, 
    ///     <see cref="IProjectTreeService"/> and <see cref="IProjectItemProvider"/> interfaces.
    /// </remarks>
    internal interface IPhysicalProjectTreeStorage
    {
        /// <summary>
        ///     Creates a folder on disk, adding it add to the physical project tree.
        /// </summary>
        /// <param name="path">
        ///     The path of the folder to create, can be relative to the project directory.
        /// </param>
        /// <returns>
        ///     The created <see cref="IProjectTree"/>.
        /// </returns>
        /// <remarks>
        ///     This method will automatically publish the resulting tree to <see cref="IProjectTreeService.CurrentTree"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path"/> is an empty string ("").
        /// </exception>
        Task<IProjectTree> CreateFolderAsync(string path);
    }
}
