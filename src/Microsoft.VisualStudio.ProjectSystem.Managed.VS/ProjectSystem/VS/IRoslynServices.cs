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
        /// <param name="oldName">The old name for the type.</param>
        /// <param name="newName">The new name for the type.</param>
        /// <param name="filePath">The path to the file that contains the type.</param>
        /// <returns>True if there exists types that should be renamed, false if there do not.</returns>
        Task<bool> AnyTypeToRenameAsync(string oldName, string newName, string filePath);

        /// <summary>
        ///  Rename the type in the given solution.
        /// </summary>
        /// <param name="oldName">The old name for the type.</param>
        /// <param name="newName">The new name for the type.</param>
        /// <param name="filePath">The path to the file that contains the type.</param>
        /// <param name="cancellationToken">A token that can be used to cancel this operation.</param>
        /// <returns>True if the changes could be applied, false if they could not.</returns>
        Task<bool> RenameTypeAsync(string oldName, string newName, string filePath, CancellationToken cancellationToken);
    }
}
