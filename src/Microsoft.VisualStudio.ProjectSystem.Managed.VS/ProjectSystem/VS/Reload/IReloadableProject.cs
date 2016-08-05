// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Interface passed to the IProjectReloadManager by reloadable projects. Used by the ProjectReloadManager to 
    /// have the project reload itself from a mewer project file om disk
    /// </summary>
    internal interface IReloadableProject
    {
        string ProjectFile { get; }
        IVsHierarchy VsHierarchy { get; }

        Task<ProjectReloadResult> ReloadProjectAsync();
    }
}