// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal interface IRoslynServices
    {
        Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName);
        bool ApplyChangesToSolution(Workspace ws, Solution renamedSolution);
        bool IsValidIdentifier(string identifierName);
    }
}
