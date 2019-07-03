// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.ProjectSystem.Refactor;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Refactor
{
    internal sealed partial class VisualStudioRefactorNotifyService
    {
        internal class Instance : IMultiLifetimeInstance, IRefactorNotifyService
        {
            private readonly IVsService<SDTE, DTE> _dte;
            private readonly IVsService<SVsSolution, IVsSolution> _solutionService;

            public Instance(IVsService<SDTE, DTE> dte, IVsService<SVsSolution, IVsSolution> solutionService)
            {
                _dte = dte;
                _solutionService = solutionService;
            }

            public async Task TryOnBeforeGlobalSymbolRenamedAsync(string projectPath, IEnumerable<string> filePaths, string rqName, string newName)
            {
                IVsHierarchy projectHierarchy = await GetProjectHierarchy(projectPath);
                if (projectHierarchy == null)
                {
                    return;
                }

                if (!(projectHierarchy is IVsHierarchyRefactorNotify refactorNotify))
                {
                    return;
                }

                var ids = GetIdsForFiles(projectHierarchy, filePaths).ToArray();

                refactorNotify.OnBeforeGlobalSymbolRenamed(cItemsAffected: (uint)ids.Length,
                                                           rgItemsAffected: ids,
                                                           cRQNames: (uint)1,
                                                           rglpszRQName: new[] { rqName },
                                                           lpszNewName: newName,
                                                           promptContinueOnFail: 1);
            }

            public async Task TryOnAfterGlobalSymbolRenamedAsync(string projectPath, IEnumerable<string> filePaths, string rqName, string newName)
            {
                IVsHierarchy projectHierarchy = await GetProjectHierarchy(projectPath);
                if (projectHierarchy == null)
                {
                    return;
                }

                if (!(projectHierarchy is IVsHierarchyRefactorNotify refactorNotify))
                {
                    return;
                }

                var ids = GetIdsForFiles(projectHierarchy, filePaths).ToArray();

                refactorNotify.OnGlobalSymbolRenamed(cItemsAffected: (uint)ids.Length,
                                                     rgItemsAffected: ids,
                                                     cRQNames: (uint)1,
                                                     rglpszRQName: new[] { rqName },
                                                     lpszNewName: newName);
            }

            private async Task<IVsHierarchy> GetProjectHierarchy(string projectPath)
            {
                Project project = await TryGetProjectFromPathAsync(projectPath);
                if (project == null)
                {
                    return null;
                }

                return await TryGetIVsHierarchyAsync(project);
            }

            private async Task<Project> TryGetProjectFromPathAsync(string projectPath)
            {
                DTE dte = await _dte.GetValueAsync();
                foreach (Project project in dte.Solution.Projects)
                {
                    if (StringComparers.Paths.Equals(project.FullName, projectPath))
                    {
                        return project;
                    }
                }

                return null;
            }

            private async Task<IVsHierarchy> TryGetIVsHierarchyAsync(Project project)
            {
                var solutionService = await _solutionService.GetValueAsync();
                if (solutionService.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy projectHierarchy) == HResult.OK)
                {
                    return projectHierarchy;
                }

                return null;
            }

            private IEnumerable<uint> GetIdsForFiles(IVsHierarchy projectHierarchy, IEnumerable<string> filePaths)
            {
                foreach (var filePath in filePaths)
                {
                    if (projectHierarchy.ParseCanonicalName(filePath, out uint id) == HResult.OK)
                    {
                        yield return id;
                    }
                }
            }

            public Task InitializeAsync() => Task.CompletedTask;
            public Task DisposeAsync() => Task.CompletedTask;
        }
    }
}
