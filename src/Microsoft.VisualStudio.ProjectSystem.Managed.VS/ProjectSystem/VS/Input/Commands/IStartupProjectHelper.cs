// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Provides a mechanism to get an export from a DotNet project which is the only startup project. The capabilityMatch is
    /// used to refine the projects that are considered
    /// </summary>
    internal interface IStartupProjectHelper
    {
        ImmutableArray<T> GetExportFromDotNetStartupProjects<T>(string capabilityMatch) where T : class;
    }
}
