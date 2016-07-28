// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Interface used by reloadable projects to register and unregister their project files. The 
    /// project reload manager will take care of watching the file and calling the reloadable project to perform
    /// the actual reload.
    /// </summary>
    internal interface IProjectReloadManager
    {
        Task RegisterProjectAsync(IReloadableProject project);
        Task UnregisterProjectAsync(IReloadableProject project);
    }
}