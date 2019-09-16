// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IRoslynServices
    {
        Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName, CancellationToken token = default);

        bool ApplyChangesToSolution(Workspace ws, Solution renamedSolution);

        bool IsValidIdentifier(string identifierName);
    }
}
