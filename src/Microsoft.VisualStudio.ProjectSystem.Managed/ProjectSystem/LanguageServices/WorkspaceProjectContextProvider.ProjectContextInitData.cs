// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceProjectContextProvider
    {
        /// <summary>
        ///     Contains initialization data for creating a <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        private struct ProjectContextInitData
        {
            public string LanguageName;
            public string BinOutputPath;
            public string ProjectFilePath;
            public string AssemblyName;
            public Guid ProjectGuid;
            public string WorkspaceProjectContextId;

            public bool IsInvalid()
            {
                // If we don't have these MSBuild values as a minimum, we cannot create a context.
                if (string.IsNullOrEmpty(LanguageName) || string.IsNullOrEmpty(BinOutputPath) || string.IsNullOrEmpty(ProjectFilePath))
                    return true;

                Assumes.False(ProjectGuid == Guid.Empty);
                Assumes.NotNull(WorkspaceProjectContextId);

                return false;
            }

            public static ProjectContextInitData GetProjectContextInitData(IProjectRuleSnapshot snapshot, Guid projectGuid, ProjectConfiguration configuration)
            {
                var data = new ProjectContextInitData();

                snapshot.Properties.TryGetValue(ConfigurationGeneral.LanguageServiceNameProperty, out data.LanguageName);
                snapshot.Properties.TryGetValue(ConfigurationGeneral.TargetPathProperty, out data.BinOutputPath);
                snapshot.Properties.TryGetValue(ConfigurationGeneral.MSBuildProjectFullPathProperty, out data.ProjectFilePath);
                snapshot.Properties.TryGetValue(ConfigurationGeneral.AssemblyNameProperty, out data.AssemblyName);

                data.ProjectGuid = projectGuid;
                data.WorkspaceProjectContextId = GetWorkspaceProjectContextId(data.ProjectFilePath, projectGuid, configuration);

                return data;
            }

            private static string GetWorkspaceProjectContextId(string projectFilePath, Guid projectGuid, ProjectConfiguration configuration)
            {
                // WorkspaceContextId must be unique across the entire solution for the life of the solution, therefore as we fire 
                // up a workspace context per implicitly active config, we factor in both the full path of the project, the GUID of 
                // project and the name of the config. This will be unique across regardless of whether projects are added or renamed 
                // to match this project's original name. We include file path to make debugging easier on the Roslyn side.
                //
                // NOTE: Roslyn also uses this name as the default "AssemblyName" until we explicitly set it, so we need to make 
                // sure it doesn't contain any invalid path characters.
                //
                // For example:
                //      C:\Project\Project.csproj (Debug;AnyCPU {72B509BD-C502-4707-ADFD-E2D43867CF45})
                //      C:\Project\MultiTarget.csproj (Debug;AnyCPU;net45 {72B509BD-C502-4707-ADFD-E2D43867CF45})

                return $"{projectFilePath} ({configuration.Name.Replace("|", ";")} {projectGuid.ToString("B").ToUpperInvariant()})";
            }
        }
    }
}
