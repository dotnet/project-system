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
        private static readonly ReferenceCleanUpInvoker s_commandInvoker = new ();

        private static readonly Dictionary<ProjectSystemReferenceType, AbstractReferenceHandler> s_mapReferenceTypeToHandler =
            new ()
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

        /// <summary>
        /// Return the set of direct Project and Package References for the given project. This
        /// is used to get the initial state of the TreatAsUsed attribute for each reference.
        /// </summary>
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

        /// <summary>
        /// Updates the project’s references by removing or marking references as
        /// TreatAsUsed in the project file.
        /// </summary>
        /// <returns>True, if the reference was updated.</returns>
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
            bool wasUpdated = true;

            cancellationToken.ThrowIfCancellationRequested();
            switch (referenceUpdate.Action)
            {
                case ProjectSystemUpdateAction.SetTreatAsUsed:
                case ProjectSystemUpdateAction.UnsetTreatAsUsed:
                    await s_commandInvoker.ExecuteCommandAsync(referenceHandler.CreateUpdateReferenceCommand(selectedConfiguredProject, referenceUpdate));
                    break;
                case ProjectSystemUpdateAction.Remove:
                    await s_commandInvoker.ExecuteCommandAsync(referenceHandler.CreateRemoveReferenceCommand(selectedConfiguredProject, referenceUpdate));
                    break;
                case (ProjectSystemUpdateAction)3:
                    await s_commandInvoker.UndoCommand();
                    break;
                case (ProjectSystemUpdateAction)4:
                    await s_commandInvoker.RedoCommand();
                    break;
                default:
                    wasUpdated = false;
                    break;
            }
            
            return wasUpdated;
        }
    }
}
