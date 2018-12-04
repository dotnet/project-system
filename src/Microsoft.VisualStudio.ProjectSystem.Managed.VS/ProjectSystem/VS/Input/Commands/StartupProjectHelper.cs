// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Buffers.PooledObjects;
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
        /// Returns the export T of the startup projects if those projects support the specified capabilities
        /// </summary>
        public ImmutableArray<T> GetExportFromDotNetStartupProjects<T>(string capabilityMatch) where T : class
        {
            EnvDTE.DTE dte = ServiceProvider.GetService<EnvDTE.DTE, EnvDTE.DTE>();
            if (dte != null)
            {
                if (dte.Solution.SolutionBuild.StartupProjects is Array startupProjects && startupProjects.Length > 0)
                {
                    IVsSolution sln = ServiceProvider.GetService<IVsSolution, SVsSolution>();
                    var results = PooledArray<T>.GetInstance();
                    foreach (string projectName in startupProjects)
                    {
                        sln.GetProjectOfUniqueName(projectName, out IVsHierarchy hier);
                        if (hier != null && hier.IsCapabilityMatch(capabilityMatch))
                        {
                            string projectPath = hier.GetProjectFilePath();
                            results.Add(ProjectExportProvider.GetExport<T>(projectPath));
                        }
                    }
                    return results.ToImmutableAndFree();
                }
            }
            return ImmutableArray<T>.Empty;
        }
    }
}
