// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
