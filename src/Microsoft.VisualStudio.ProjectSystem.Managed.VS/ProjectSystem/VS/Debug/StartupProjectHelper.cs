// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EnvDTE;
using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
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
            if (_dte.Value.Solution.SolutionBuild.StartupProjects is Array { Length: > 0 } startupProjects)
            {
                var results = PooledArray<T>.GetInstance();

                foreach (string projectName in startupProjects)
                {
                    _solution.Value.GetProjectOfUniqueName(projectName, out IVsHierarchy hier);

                    if (hier?.IsCapabilityMatch(capabilityMatch) == true)
                    {
                        string? projectPath = hier.GetProjectFilePath();

                        if (projectPath is not null)
                        {
                            T? export = _projectExportProvider.GetExport<T>(projectPath);

                            if (export is not null)
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

        public ImmutableArray<string> GetFullPathsOfStartupProjects()
        {
            if (_dte.Value.Solution.SolutionBuild.StartupProjects is Array { Length: > 0 } startupProjects)
            {
                var results = PooledArray<string>.GetInstance(startupProjects.Length);

                foreach (string projectName in startupProjects)
                {
                    _solution.Value.GetProjectOfUniqueName(projectName, out IVsHierarchy hier);

                    string? projectPath = hier?.GetProjectFilePath();

                    if (projectPath is not null)
                    {
                        results.Add(projectPath);
                    }
                }

                return results.ToImmutableAndFree();
            }

            return ImmutableArray<string>.Empty;
        }
    }
}
