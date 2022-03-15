// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    /// <summary>
    /// This is to implement the command design pattern to make changes
    /// to references in the csproj file.
    /// </summary>
    internal interface IReferenceCommand
    {
        Task ExecuteAsync();

        Task UndoAsync();

        Task RedoAsync();
    }
}
