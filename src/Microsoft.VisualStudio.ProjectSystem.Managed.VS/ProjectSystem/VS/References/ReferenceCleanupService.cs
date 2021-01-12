// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Exceptions;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    [Export(typeof(IProjectSystemReferenceCleanupService))]
    internal class ReferenceCleanupService : IProjectSystemReferenceCleanupService
    {
        private static readonly Dictionary<ProjectSystemReferenceType, AbstractReferenceHandler> s_mapReferenceTypeToHandler =
            new Dictionary<ProjectSystemReferenceType, AbstractReferenceHandler>()
            {
                { ProjectSystemReferenceType.Project, new ProjectReferenceHandler() },
                { ProjectSystemReferenceType.Package, new PackageReferenceHandler() },
                { ProjectSystemReferenceType.Assembly, new AssemblyReferenceHandler() }
            };

        private readonly Lazy<IProjectService2> _projectService;
        protected IProjectService2 ProjectService => _projectService.Value;

        [ImportingConstructor]
        public ReferenceCleanupService(IProjectServiceAccessor projectServiceAccessor)
        {
            _projectService = new Lazy<IProjectService2>(
                () => (IProjectService2)projectServiceAccessor.GetProjectService(),
                LazyThreadSafetyMode.PublicationOnly);
        }

        public async Task<ImmutableArray<ProjectSystemReferenceInfo>> GetProjectReferencesAsync(string projectPath, CancellationToken cancellationToken)
        {
            List<ProjectSystemReferenceInfo> references;

            try
            {
                var activeConfiguredProject = await GetActiveConfiguredProjectByPathAsync(projectPath, cancellationToken);

                references = await GetAllReferencesInConfiguredProjectAsync(activeConfiguredProject, cancellationToken);
            }
            catch
            {
                throw new InvalidProjectFileException();
            }

            return references.ToImmutableArray();
        }

        private async Task<ConfiguredProject> GetActiveConfiguredProjectByPathAsync(string projectPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var unconfiguredProjectPath = ProjectService.GetLoadedProject(projectPath);

            if (unconfiguredProjectPath is null)
            {
                throw new InvalidProjectFileException();
            }

            var activeConfiguredProject = await unconfiguredProjectPath.GetSuggestedConfiguredProjectAsync();

            if (activeConfiguredProject is null)
            {
                throw new InvalidProjectFileException();
            }

            return activeConfiguredProject;
        }

        private static async Task<List<ProjectSystemReferenceInfo>> GetAllReferencesInConfiguredProjectAsync(ConfiguredProject selectedConfiguredProject, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var references = new List<ProjectSystemReferenceInfo>();

            foreach (var keyValuePair in s_mapReferenceTypeToHandler.Where(h => h.Value != null))
            {
                references.AddRange(await keyValuePair.Value.GetReferencesAsync(selectedConfiguredProject, cancellationToken));
            }

            return references;
        }

        public async Task<bool> TryUpdateReferenceAsync(string projectPath, ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
        {
            bool wasUpdated = false;

            cancellationToken.ThrowIfCancellationRequested();

            var activeConfiguredProject = await GetActiveConfiguredProjectByPathAsync(projectPath, cancellationToken);

            var referenceHandler = s_mapReferenceTypeToHandler[referenceUpdate.ReferenceInfo.ReferenceType];

            if (referenceHandler != null)
            {
                wasUpdated = await ApplyActionAsync(referenceUpdate, referenceHandler, activeConfiguredProject, cancellationToken);
            }

            return wasUpdated;
        }

        private static async Task<bool> ApplyActionAsync(ProjectSystemReferenceUpdate referenceUpdate, AbstractReferenceHandler referenceHandler, 
            ConfiguredProject selectedConfiguredProject, CancellationToken cancellationToken)
        {
            bool wasUpdated = false;

            cancellationToken.ThrowIfCancellationRequested();

            if (referenceUpdate.Action == ProjectSystemUpdateAction.SetTreatAsUsed ||
                referenceUpdate.Action == ProjectSystemUpdateAction.UnsetTreatAsUsed)
            {
                wasUpdated =
                    await referenceHandler.UpdateReferenceAsync(selectedConfiguredProject, referenceUpdate, cancellationToken);
            }
            else
            {
                if (await referenceHandler.CanRemoveReferenceAsync(selectedConfiguredProject, referenceUpdate, cancellationToken))
                {
                    await referenceHandler.RemoveReferenceAsync(selectedConfiguredProject, referenceUpdate.ReferenceInfo);
                    wasUpdated = true;
                }
            }

            return wasUpdated;
        }
    }
}
