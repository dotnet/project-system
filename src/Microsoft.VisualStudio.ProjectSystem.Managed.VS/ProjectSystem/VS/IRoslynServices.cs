// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// An abstraction over roslyn services.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IRoslynServices
    {
        /// <summary>
        /// Rename the symbol in the given solution.
        /// </summary>
        /// <param name="solution">The solution in which to perform the rename.</param>
        /// <param name="symbol">The symbol to rename.</param>
        /// <param name="newName">The new name for the symbol.</param>
        /// <param name="cancellationToken">A token that can be used to cancel this operation.</param>
        /// <returns>The new solution with the renamed symbol.</returns>
        Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName, CancellationToken cancellationToken);

        /// <summary>
        /// Try to apply the solution changes to the workspace.
        /// </summary>
        /// <param name="ws">The workspace to apply the changes into.</param>
        /// <param name="renamedSolution">The solution containing the changes.</param>
        /// <returns>True if the changes could be applied, false if they could not.</returns>
        bool ApplyChangesToSolution(Workspace ws, Solution renamedSolution);

        /// <summary>
        /// Determines if the given string is a valid identifier within the current programming language.
        /// </summary>
        /// <param name="identifierName">A string representing the identifier name.</param>
        /// <returns>A boolean indicating that the string is a valid identifier if true.</returns>
        bool IsValidIdentifier(string identifierName);
    }
}
