// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
