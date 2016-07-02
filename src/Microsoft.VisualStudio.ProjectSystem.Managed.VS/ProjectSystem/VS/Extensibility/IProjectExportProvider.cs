// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Extensibility
{
    /// <summary>
    /// Interface definition for global scope VS MEF component, which helps to get MEF exports from a
    /// project level scope given IVsHierarchy or project file path.
    /// </summary>
    public interface IProjectExportProvider
    {
        /// <summary>
        /// This function requires going to the UI thread to map the hierarchy to the correct project
        /// instance. 
        /// </summary>
        T GetExport<T>(IVsHierarchy projectHierarchy) where T : class;

        /// <summary>
        /// Returns the export for the given project without having to go to the 
        /// UI thread. This is the preferred method for getting access to project specific
        /// exports
        /// </summary>
        T GetExport<T>(string projectFilePath) where T : class;
    }
}
