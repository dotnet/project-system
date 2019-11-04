// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using EnvDTE;
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
        private readonly IVsUIService<DTE> _dte;
        private readonly IVsUIService<IVsSolution> _solution;
        private readonly IProjectExportProvider _projectExportProvider;

        [ImportingConstructor]
        public StartupProjectHelper(IVsUIService<SDTE, DTE> dte,
                                    IVsUIService<SVsSolution, IVsSolution> solution,
                                    IProjectExportProvider projectExportProvider)
        {
            _dte = dte;
            _solution = solution;
            _projectExportProvider = projectExportProvider;
        }

        /// <summary>
        /// Returns the export T of the startup projects if those projects support the specified capabilities
        /// </summary>
        public ImmutableArray<T> GetExportFromDotNetStartupProjects<T>(string capabilityMatch) where T : class
        {
            DTE? dte = _dte.Value;
            Assumes.Present(dte);

            if (dte.Solution.SolutionBuild.StartupProjects is Array startupProjects && startupProjects.Length > 0)
            {
                IVsSolution? solution = _solution.Value;
                Assumes.Present(solution);

                var results = PooledArray<T>.GetInstance();

                foreach (string projectName in startupProjects)
                {
                    solution.GetProjectOfUniqueName(projectName, out IVsHierarchy hier);

                    if (hier != null && hier.IsCapabilityMatch(capabilityMatch))
                    {
                        string? projectPath = hier.GetProjectFilePath();

                        if (projectPath != null)
                        {
                            T? export = _projectExportProvider.GetExport<T>(projectPath);

                            if (export != null)
                            {
                                results.Add(export);
                            }
                        }
                    }
                }

                return results.ToImmutableAndFree();
            }

            return ImmutableArray<T>.Empty;
        }
    }
}
