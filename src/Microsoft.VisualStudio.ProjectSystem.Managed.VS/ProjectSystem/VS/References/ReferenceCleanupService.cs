// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable disable

using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Exceptions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    [Export(typeof(IProjectSystemReferenceCleanupService))]
    internal partial class ReferenceCleanupService : IProjectSystemReferenceCleanupService
    {
        private readonly AbstractReferenceHandler _projectAbstractReferenceHandler = new ProjectAbstractReferenceHandler();
        private readonly AbstractReferenceHandler _packageAbstractReferenceHandler = new PackageAbstractReferenceHandler();
        private readonly AbstractReferenceHandler _assemblyAbstractReferenceHandler = new AssemblyAbstractReferenceHandler();

        private readonly Dictionary<ProjectSystemReferenceType, AbstractReferenceHandler> _mapReferenceTypeToHandler;

        private readonly IProjectServiceAccessor _projectServiceAccessor;
        private readonly IVsUIService<DTE> _dte;
        private readonly IVsUIService<SVsSolution, IVsSolution> _solution;

        [ImportingConstructor]
        public ReferenceCleanupService(IProjectServiceAccessor projectServiceAccessor, IVsUIService<SDTE, DTE> dte, IVsUIService<SVsSolution, IVsSolution> solution)
        {
            _projectServiceAccessor = projectServiceAccessor;
            _dte = dte;
            _solution = solution;

            _mapReferenceTypeToHandler =
                new Dictionary<ProjectSystemReferenceType, AbstractReferenceHandler>()
                {
                    { ProjectSystemReferenceType.Project, _projectAbstractReferenceHandler },
                    { ProjectSystemReferenceType.Package, _packageAbstractReferenceHandler },
                    { ProjectSystemReferenceType.Assembly, _assemblyAbstractReferenceHandler},
                    { ProjectSystemReferenceType.Unknown, null }
                };
        }

        public Task<string> GetProjectAssetsFilePathAsync(string projectPath, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<ImmutableArray<ProjectSystemReferenceInfo>> GetProjectReferencesAsync(string projectPath, string targetFrameworkMoniker, CancellationToken cancellationToken)
        {
            List<ProjectSystemReferenceInfo> references;

            try
            {
                var activeConfiguredProject = await GetActiveConfiguredProjectByPathAsync(projectPath);

                references = GetAllReferencesInConfiguredProject(activeConfiguredProject);
            }
            catch
            {
                throw new InvalidProjectFileException();
            }

            return references.ToImmutableArray();
        }

        private async Task<ConfiguredProject> GetActiveConfiguredProjectByPathAsync(string projectPath)
        {
            var unconfiguredProjectPath = FindUnconfiguredProjectByPath(projectPath);

            var activeConfiguredProject = await unconfiguredProjectPath.GetSuggestedConfiguredProjectAsync();
            if (activeConfiguredProject is null)
            {
                throw new InvalidProjectFileException();
            }

            return activeConfiguredProject;
        }

        private UnconfiguredProject FindUnconfiguredProjectByPath(string projectPath)
        {
            var projectService = _projectServiceAccessor.GetProjectService();

            var unconfigProjects = projectService.LoadedUnconfiguredProjects;

            var unconfiguredProject = unconfigProjects.First(project =>
                StringComparers.Paths.Equals((string)project.FullPath, projectPath));

            return unconfiguredProject;
        }

        private List<ProjectSystemReferenceInfo> GetAllReferencesInConfiguredProject(ConfiguredProject selectedConfiguredProject)
        {
            GetProjectSnapshot(selectedConfiguredProject);

            var references = GetReferences();

            return references;
        }

        private void GetProjectSnapshot(ConfiguredProject selectedConfiguredProject)
        {
            _projectAbstractReferenceHandler.GetProjectSnapshot(selectedConfiguredProject);
            _packageAbstractReferenceHandler.GetProjectSnapshot(selectedConfiguredProject);
            _assemblyAbstractReferenceHandler.GetProjectSnapshot(selectedConfiguredProject);
        }

        private List<ProjectSystemReferenceInfo> GetReferences()
        {
            var references = new List<ProjectSystemReferenceInfo>();

            references.AddRange(_projectAbstractReferenceHandler.GetReferences());
            references.AddRange(_packageAbstractReferenceHandler.GetReferences());
            references.AddRange(_assemblyAbstractReferenceHandler.GetReferences());

            return references;
        }

        public async Task<bool> TryUpdateReferenceAsync(string projectPath, string targetFrameworkMoniker, ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
        {
            bool wasUpdated = false;

            if (referenceUpdate.Action == ProjectSystemUpdateAction.None)
            {
                return wasUpdated;
            }

            var activeConfiguredProject = await GetActiveConfiguredProjectByPathAsync(projectPath);

            var referenceHandler = _mapReferenceTypeToHandler[referenceUpdate.ReferenceInfo.ReferenceType];

            if (referenceHandler != null)
            {
                wasUpdated = await ApplyAction(referenceUpdate, cancellationToken, referenceHandler, activeConfiguredProject);
            }

            return wasUpdated;
        }

        private static async Task<bool> ApplyAction(ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken,
            AbstractReferenceHandler abstractReferenceHandler, ConfiguredProject activeConfiguredProject)
        {
            bool wasUpdated = false;

            if (referenceUpdate.Action == ProjectSystemUpdateAction.TreatAsUsed ||
                referenceUpdate.Action == ProjectSystemUpdateAction.TreatAsUnused)
            {
                wasUpdated =
                    await abstractReferenceHandler.UpdateReferenceAsync(activeConfiguredProject, referenceUpdate, cancellationToken);
            }
            else
            {
                if (CanRemoveReference(referenceUpdate, abstractReferenceHandler))
                {
                    wasUpdated =
                        await abstractReferenceHandler.RemoveReferenceAsync(activeConfiguredProject, referenceUpdate.ReferenceInfo);
                }
            }

            return wasUpdated;
        }

        private static bool CanRemoveReference(ProjectSystemReferenceUpdate referenceUpdate, AbstractReferenceHandler abstractReferenceHandler)
        {
            var references = abstractReferenceHandler.GetReferences();

            var reference = references.First(c => c.ItemSpecification == referenceUpdate.ReferenceInfo.ItemSpecification);

            return reference.TreatAsUsed != true;
        }

        public bool IsProjectCpsBased(string projectPath)
        {
            var projectHierarchy = GetProjectHierarchy(projectPath);
            var isCps = projectHierarchy.IsCapabilityMatch(ProjectCapabilities.Cps);

            return isCps;
        }

        private IVsHierarchy GetProjectHierarchy(string projectPath)
        {
            var project = TryGetProjectFromPath(projectPath);
            if (project == null)
            {
                return null;
            }

            return TryGetIVsHierarchy(project);
        }

        private Project TryGetProjectFromPath(string projectPath)
        {
            foreach (Project project in _dte.Value.Solution.Projects)
            {
                string fullName;
                try
                {
                    fullName = project.FullName;
                }
                catch (Exception)
                {
                    // DTE COM calls can fail for any number of valid reasons.
                    continue;
                }

                if (StringComparers.Paths.Equals(fullName, projectPath))
                {
                    return project;
                }
            }

            return null;
        }

        private IVsHierarchy TryGetIVsHierarchy(Project project)
        {
            if (_solution.Value.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy projectHierarchy) == HResult.OK)
            {
                return projectHierarchy;
            }

            return null;
        }
    }
}
