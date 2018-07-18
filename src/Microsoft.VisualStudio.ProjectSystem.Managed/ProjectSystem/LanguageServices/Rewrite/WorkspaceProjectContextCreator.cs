// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Responsible for creating <see cref="IWorkspaceProjectContext"/> instances based on specified inputs.
    /// </summary>
    internal partial class WorkspaceProjectContextCreator
    {
        private readonly ConfiguredProject _project;
        private readonly Lazy<IWorkspaceProjectContextFactory> _workspaceProjectContextFactory;
        private readonly Lazy<ISafeProjectGuidService> _projectGuidService;
        private readonly IUnconfiguredProjectCommonServices _projectServices;

        [ImportingConstructor]
        public WorkspaceProjectContextCreator(ConfiguredProject project,
                                             Lazy<IWorkspaceProjectContextFactory> workspaceProjectContextFactory,
                                             Lazy<ISafeProjectGuidService> projectGuidService,
                                             IUnconfiguredProjectCommonServices projectServices)
        {
            _project = project;
            _workspaceProjectContextFactory = workspaceProjectContextFactory;
            _projectGuidService = projectGuidService;
            _projectServices = projectServices;
        }

        public async Task<IWorkspaceProjectContext> CreateProjectContext(string languageName, string binOutputPath, string projectFilePath)
        {
            Requires.NotNullOrEmpty(projectFilePath, nameof(projectFilePath));

            Guid projectGuid = await _projectGuidService.Value.GetProjectGuidAsync()
                                                              .ConfigureAwait(true);

            Assumes.False(projectGuid == Guid.Empty);

            // If these properties (coming from MSBuild) are empty, return a "null" project context
            if (string.IsNullOrEmpty(languageName) || string.IsNullOrEmpty(binOutputPath))
                return NullWorkspaceProjectContext.Instance;

            string workspaceProjectContextId = GetWorkspaceProjectContextId(projectFilePath, _project.ProjectConfiguration);
            object hostObject = _projectServices.Project.Services.HostObject;

            try
            {
                return _workspaceProjectContextFactory.Value.CreateProjectContext(languageName, workspaceProjectContextId, projectFilePath, projectGuid, hostObject, binOutputPath);
            }
            catch (Exception)
            {   // TODO: Watson
            }

            return NullWorkspaceProjectContext.Instance;
        }

        private static string GetWorkspaceProjectContextId(string filePath, ProjectConfiguration projectConfiguration)
        {
            // WorkspaceContextId must be unique across the entire solution, therefore as we fire up a workspace context 
            // per implicitly active config, we factor in both the full path of the project + the name of the config.
            //
            // NOTE: Roslyn also uses this name as the default "AssemblyName" until we explicitly set it, so we need to make 
            // sure it doesn't contain any invalid path characters.
            //
            // For example:
            //      C:\Project\Project.csproj (Debug_AnyCPU)
            //      C:\Project\MultiTarget.csproj (Debug_AnyCPU_net45)

            // TODO: filePath isn't stable
            return $"{filePath} ({projectConfiguration.Name.Replace("|", "_")})";
        }
    }
}
