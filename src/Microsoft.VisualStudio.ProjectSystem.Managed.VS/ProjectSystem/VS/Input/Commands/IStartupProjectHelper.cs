// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Provides a mechanism to get an export from a DotNet project which is the only startup project. The capabilityMatch is  
    /// used to refine the projects that are considered
    /// </summary>
    internal interface IStartupProjectHelper
    {
        T GetExportFromSingleDotNetStartupProject<T>(string capabilityMatch) where T : class;
    }
}
