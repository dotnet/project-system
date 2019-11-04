// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.ProjectSystem.Refactor;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Refactor
{
    [Export(typeof(IRefactorNotifyService))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class VisualStudioRefactorNotifyService : IRefactorNotifyService
    {
        private readonly IVsUIService<DTE> _dte;
        private readonly IVsUIService<IVsSolution> _solution;

        [ImportingConstructor]
        public VisualStudioRefactorNotifyService(IVsUIService<SDTE, DTE> dte, IVsUIService<SVsSolution, IVsSolution> solution)
        {
            _dte = dte;
            _solution = solution;
        }

        public void OnBeforeGlobalSymbolRenamed(string projectPath, IEnumerable<string> filePaths, string rqName, string newName)
        {
            IVsHierarchy? projectHierarchy = GetProjectHierarchy(projectPath);
            if (projectHierarchy == null)
            {
                return;
            }

            if (!(projectHierarchy is IVsHierarchyRefactorNotify refactorNotify))
            {
                return;
            }

            uint[] ids = GetIdsForFiles(projectHierarchy, filePaths).ToArray();

            refactorNotify.OnBeforeGlobalSymbolRenamed(cItemsAffected: (uint)ids.Length,
                                                       rgItemsAffected: ids,
                                                       cRQNames: 1,
                                                       rglpszRQName: new[] { rqName },
                                                       lpszNewName: newName,
                                                       promptContinueOnFail: 1);
        }

        public void OnAfterGlobalSymbolRenamed(string projectPath, IEnumerable<string> filePaths, string rqName, string newName)
        {
            IVsHierarchy? projectHierarchy = GetProjectHierarchy(projectPath);
            if (projectHierarchy == null)
            {
                return;
            }

            if (!(projectHierarchy is IVsHierarchyRefactorNotify refactorNotify))
            {
                return;
            }

            uint[] ids = GetIdsForFiles(projectHierarchy, filePaths).ToArray();

            refactorNotify.OnGlobalSymbolRenamed(cItemsAffected: (uint)ids.Length,
                                                 rgItemsAffected: ids,
                                                 cRQNames: 1,
                                                 rglpszRQName: new[] { rqName },
                                                 lpszNewName: newName);
        }

        private IVsHierarchy? GetProjectHierarchy(string projectPath)
        {
            Project? project = TryGetProjectFromPath(projectPath);
            if (project == null)
            {
                return null;
            }

            return TryGetIVsHierarchy(project);
        }

        private Project? TryGetProjectFromPath(string projectPath)
        {
            DTE? dte = _dte.Value;
            Assumes.NotNull(dte);

            foreach (Project project in dte.Solution.Projects)
            {
                string? fullName;
                try
                {
                    fullName = project.FullName;
                }
                catch (Exception)
                {
                    // DTE COM calls can fail for any number of valid reasions.
                    continue;
                }

                if (StringComparers.Paths.Equals(fullName, projectPath))
                {
                    return project;
                }
            }

            return null;
        }

        private IVsHierarchy? TryGetIVsHierarchy(Project project)
        {
            IVsSolution? solution = _solution.Value;
            Assumes.Present(solution);

            if (solution.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy projectHierarchy) == HResult.OK)
            {
                return projectHierarchy;
            }

            return null;
        }

        private static IEnumerable<uint> GetIdsForFiles(IVsHierarchy projectHierarchy, IEnumerable<string> filePaths)
        {
            foreach (string filePath in filePaths)
            {
                if (projectHierarchy.ParseCanonicalName(filePath, out uint id) == HResult.OK)
                {
                    yield return id;
                }
            }
        }
    }
}
