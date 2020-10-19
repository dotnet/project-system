// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Exceptions;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.References.Roslyn;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    [Export(typeof(IReferenceCleanupService))]
    internal partial class ReferenceCleanupService : IReferenceCleanupService
    {
        ReferenceHandler _projectReferenceHandler = new ProjectReferenceHandler();
        ReferenceHandler _packageReferenceHandler = new PackageReferenceHandler();
        ReferenceHandler _assemblyReferenceHandler = new AssemblyReferenceHandler();
        ReferenceHandler _sdkReferenceHandler = new SdkReferenceHandler();

        private readonly ConfiguredProject _configuredProject;
        private readonly IVsUIService<DTE> _dte;
        private readonly IVsUIService<SVsSolution, IVsSolution> _solution;

        private Dictionary<ReferenceType, string> _referenceTypes = new Dictionary<ReferenceType, string>();

        [ImportingConstructor]
        public ReferenceCleanupService(ConfiguredProject configuredProject, IVsUIService<SDTE, DTE> dte, IVsUIService<SVsSolution, IVsSolution> solution)
        {
            _configuredProject = configuredProject;
            _dte = dte;
            _solution = solution;
        }

        public Task<string> GetProjectAssetsFilePathAsync(string projectPath, string targetFramework, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<ImmutableArray<Reference>> GetProjectReferencesAsync(string projectPath, string targetFramework, CancellationToken cancellationToken)
        {
            List<Reference> references;

            try
            {
                ConfiguredProject activeConfiguredProject = await GetActiveConfiguredProjectByPathAsync(projectPath);

                references = await GetAllReferencesInConfiguredProjectAsync(activeConfiguredProject);
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
            var unconfigProjects = _configuredProject.Services.ProjectService.LoadedUnconfiguredProjects;

            UnconfiguredProject unconfiguredProject = unconfiguredProject = unconfigProjects.First(project =>
                StringComparers.Paths.Equals((string)project.FullPath, projectPath));

            return unconfiguredProject;
        }

        private async Task<List<Reference>> GetAllReferencesInConfiguredProjectAsync(ConfiguredProject selectedConfiguredProject)
        {
            GetProjectSnapshotAsync(selectedConfiguredProject);

            var references = GetReferences();

            return references;
        }

        private void GetProjectSnapshotAsync(ConfiguredProject selectedConfiguredProject)
        {
            _projectReferenceHandler.GetProjectSnapshot(selectedConfiguredProject);
            _packageReferenceHandler.GetProjectSnapshot(selectedConfiguredProject);
            _assemblyReferenceHandler.GetProjectSnapshot(selectedConfiguredProject);
            _sdkReferenceHandler.GetProjectSnapshot(selectedConfiguredProject);
        }

        private List<Reference> GetReferences()
        {
            List<Reference> references = new List<Reference>();

            _projectReferenceHandler.GetAndAppendReferences(references);
            _packageReferenceHandler.GetAndAppendReferences(references);
            _assemblyReferenceHandler.GetAndAppendReferences(references);
            _sdkReferenceHandler.GetAndAppendReferences(references);

            return references;
        }

        public async Task<bool> UpdateReferencesAsync(string projectPath, string targetFramework,
            ImmutableArray<ReferenceUpdate> referenceUpdates, CancellationToken cancellationToken)
        {
            ConfiguredProject activeConfiguredProject = await GetActiveConfiguredProjectByPathAsync(projectPath);

            referenceUpdates.Where(c => c.Action == UpdateAction.Remove).ToList().ForEach(e =>
            {
                ReferenceHandler referenceHandler = FindReferenceHandler(e);
                referenceHandler.RemoveReference(activeConfiguredProject, e.Reference);
            });

            referenceUpdates.Where(c => c.Action == UpdateAction.Add).ToList().ForEach(e =>
            {
                ReferenceHandler referenceHandler = FindReferenceHandler(e);
                referenceHandler.AddReference(activeConfiguredProject, e.Reference);
            });

            var updateActions = referenceUpdates.Where(c => c.Action == UpdateAction.Update);
            ExecuteUpdateReference(activeConfiguredProject, updateActions);
            
            return true;
        }

        private ReferenceHandler FindReferenceHandler(ReferenceUpdate referenceUpdate)
        {
            ReferenceHandler referenceHandler;
            if (referenceUpdate.Reference.Type == ReferenceType.Project)
            {
                referenceHandler = _projectReferenceHandler;
            }
            else if (referenceUpdate.Reference.Type == ReferenceType.Package)
            {
                referenceHandler = _packageReferenceHandler;
            }
            else if (referenceUpdate.Reference.Type == ReferenceType.Assembly)
            {
                referenceHandler = _assemblyReferenceHandler;
            }
            else
            {
                referenceHandler = _sdkReferenceHandler;
            }

            return referenceHandler;
        }

        private async Task ExecuteUpdateReference(ConfiguredProject activeConfiguredProject, IEnumerable<ReferenceUpdate> updateActions)
        {
            // Handle all update actions together because opening the project file for edits uses
            // locks and we don't want to delay other processes.
            string projectPath = activeConfiguredProject.UnconfiguredProject.FullPath;

            // TODO - open the project and and write to TreatAsUsed attribute using msbuild
            await activeConfiguredProject.Services.ProjectLockService.WriteLockAsync(async access =>
            {
                var projectXmlAsync = await access.GetProjectXmlAsync("");
                var items = projectXmlAsync.Items;

                foreach (var update in updateActions)
                {
                    ReferenceHandler referenceHandler = FindReferenceHandler(update);

                    foreach (var item in items)
                    {

                        if (item.ItemType == _referenceTypes[update.Reference.Type] && item.Include == update.Reference.ItemSpecification)
                        {
                            foreach (var child in item.Children)
                            {
                                if (child.ElementName == "TreatAsUsed")
                                {

                                }
                            }
                        }
                    }
                }

                access.CheckoutAsync(projectPath);
            }, CancellationToken.None);
        }

        public bool IsProjectCpsBased(string projectPath)
        {
            IVsHierarchy? projectHierarchy = GetProjectHierarchy(projectPath);
            var isCps = projectHierarchy.IsCapabilityMatch("CPS");
            return isCps;
        }

        private IVsHierarchy? GetProjectHierarchy(string projectPath)
        {
            Project project = TryGetProjectFromPath(projectPath);
            if (project == null)
            {
                return null;
            }

            return TryGetIVsHierarchy(project);
        }

        private IVsHierarchy? TryGetIVsHierarchy(Project project)
        {
            if (_solution.Value.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy projectHierarchy) == HResult.OK)
            {
                return projectHierarchy;
            }

            return null;
        }

        private Project? TryGetProjectFromPath(string projectPath)
        {
            foreach (Project project in _dte.Value.Solution.Projects)
            {
                string? fullName;
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
    }
}
