// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Singleton loaded by the managed package which tracks projects in the solution to detect if any of them are not supported
    /// with this version of Visual Studio and puts up a dialog warning the user.
    /// </summary>
    internal interface IDotNetCoreProjectCompatibilityDetector
    {
        Task InitializeAsync();
    }
}
