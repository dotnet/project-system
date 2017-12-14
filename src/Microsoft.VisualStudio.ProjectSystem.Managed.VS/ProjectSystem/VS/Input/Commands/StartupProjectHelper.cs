// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Handles populating a menu command on the debug dropdown when the menu reflects the IEnumValues for
    /// a debug property. It shows the active framework used for running the app (F5/Ctrl+F5).
    /// </summary>
    [Export(typeof(IStartupProjectHelper))]
    internal class StartupProjectHelper : IStartupProjectHelper
    {
        [ImportingConstructor]
        public StartupProjectHelper([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IProjectExportProvider projectExportProvider)
        {
            ServiceProvider = serviceProvider;
            ProjectExportProvider = projectExportProvider;
        }

        private IProjectExportProvider ProjectExportProvider { get; }
        private IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Returns the export T of the startup project if that project supports the specified capabilities
        /// </summary>
        public T GetExportFromSingleDotNetStartupProject<T>(string capabilityMatch) where T : class
        {
            EnvDTE.DTE dte = ServiceProvider.GetService<EnvDTE.DTE, EnvDTE.DTE>();
            if(dte != null)
            {
                if (dte.Solution.SolutionBuild.StartupProjects is Array startupProjects && startupProjects.Length == 1)
                {
                    IVsSolution sln = ServiceProvider.GetService<IVsSolution, SVsSolution>();
                    foreach (string projectName in startupProjects)
                    {
                        sln.GetProjectOfUniqueName(projectName, out IVsHierarchy hier);
                        if (hier != null && hier.IsCapabilityMatch(capabilityMatch))
                        {
                            string projectPath = hier.GetProjectFilePath();
                            return ProjectExportProvider.GetExport<T>(projectPath);
                        }
                    }
                }
            }
            return null;
        }   
    }
}
