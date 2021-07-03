// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// Abstraction for System.Environment for unit testing
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IEnvironmentHelper
    {
        string? GetEnvironmentVariable(string name);

        string ExpandEnvironmentVariables(string name);
    }
}
