// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// An abstraction over roslyn services.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IRenameTypeService
    {
        /// <summary>
        /// Determine if any types need to be renamed in the given file.
        /// </summary>
        /// <param name="oldFilePath">The old file path which contains types to be renamed.</param>
        /// <param name="newFilePath">The new file path which contains types to be renamed.</param>
        /// <returns>True if there exists types that should be renamed, false if there do not.</returns>
        Task<bool> AnyTypeToRenameAsync(string oldFilePath, string newFilePath);

        /// <summary>
        /// Determine if a filename and a type are currently the same
        /// </summary>
        /// <param name="filePath">The path to the file that we expect to contain the type.</param>
        /// <returns>True if there is a type contained in the file that matches the file name, otherwise false.</returns>
        Task<bool> DoesFileNameMatchTypeAsync(string filePath);

        /// <summary>
        /// Queues a type to be renamed once dataflow finishes processing it.
        /// </summary>
        /// <param name="filePath">file that containts a type we want to rename.</param>
        void QueueFileWithTypeToBeRenamed(string filePath);

        /// <summary>
        ///  Rename the type in the given solution.
        /// </summary>
        /// <param name="oldFilePath">The old file path which contains types to be renamed.</param>
        /// <param name="newFilePath">The new file path which contains types to be renamed.</param>
        /// <param name="cancellationToken">A token that can be used to cancel this operation.</param>
        /// <returns>True if the changes could be applied, false if they could not.</returns>
        bool RenameType(string oldFilePath, string newFilePath, CancellationToken cancellationToken);
    }
}
