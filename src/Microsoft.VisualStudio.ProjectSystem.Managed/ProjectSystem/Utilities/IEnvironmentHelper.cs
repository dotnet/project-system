// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// Abstraction for System.Environment for unit testing
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IEnvironmentHelper
    {
        string GetEnvironmentVariable(string name);

        string ExpandEnvironmentVariables(string name);

        bool Is64BitOperatingSystem { get; }
    }
}
