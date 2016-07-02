// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Extensibility
{
    /// <summary>
    /// MEF component which has methods for consumers to get to project specific MEF exports
    /// </summary>
    [Export(typeof(IProjectExportProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ProjectExportProvider : IProjectExportProvider
    {
        /// <summary>
        /// This function requires going to the UI thread to map the hierarchy to the correct project
        /// instance. 
        /// </summary>
        public T GetExport<T>(IVsHierarchy projectHierarchy) where T : class
        {
            T exportedValue = null;

            if (projectHierarchy == null)
            {
                return exportedValue;
            }
            try
            {
                // The following code must be run on the UI thread
                Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    UnconfiguredProject unconfigProject = projectHierarchy.GetUnconfiguredProject();
                    if (unconfigProject != null && unconfigProject.Capabilities.Contains(ProjectCapability.CSharpOrVisualBasic))
                    {
                        exportedValue = unconfigProject.Services.ExportProvider.GetExportedValue<T>();
                    }
                });
            }
            catch (Exception)
            {
                // vramak: This funtion will always returns null if the exported value is not available.
            }

            return exportedValue;
        }

        /// <summary>
        /// Returns the export for the given project without having to go to the 
        /// UI thread. This is the preferred method for getting access to project specific
        /// exports
        /// </summary>
        public T GetExport<T>(string projectFilePath) where T : class
        {
            T exportedValue = null;
            if (!string.IsNullOrEmpty(projectFilePath))
            {
                var unconfiguredProject = ActiveProjectTracker.Instance.GetProjectOfPath(projectFilePath);
                if (unconfiguredProject != null)
                {
                    try
                    {
                        exportedValue = unconfiguredProject.Services.ExportProvider.GetExportedValue<T>();
                    }
                    catch (Exception)
                    {
                        // This funtion will always returns null if the exported value is not available.
                    }
                }
            }
            return exportedValue;
        }
    }
}
