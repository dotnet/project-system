// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IRoslynServices
    {
        bool ApplyChangesToSolution(Workspace ws, Solution renamedSolution);

        bool IsValidIdentifier(string identifierName);
    }
}
