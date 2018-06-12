// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceContextHost
    {
        private partial class WorkspaceContextHostInstance
        {
            private struct ProjectData
            {
                public string LanguageName;
                public string BinOutputPath;
                public string ProjectFilePath;
                public string DisplayName;

                public static ProjectData FromSnapshot(ConfiguredProject project, IProjectRuleSnapshot snapshot)
                {
                    var data = new ProjectData();

                    snapshot.Properties.TryGetValue(ConfigurationGeneral.LanguageServiceNameProperty, out data.LanguageName);
                    snapshot.Properties.TryGetValue(ConfigurationGeneral.TargetPathProperty, out data.BinOutputPath);
                    snapshot.Properties.TryGetValue(ConfigurationGeneral.MSBuildProjectFullPathProperty, out data.ProjectFilePath);

                    data.DisplayName = GetDisplayName(data.ProjectFilePath, project.ProjectConfiguration);

                    return data;
                }

                private static string GetDisplayName(string filePath, ProjectConfiguration projectConfiguration)
                {
                    string displayName = Path.GetFileNameWithoutExtension(filePath);

                    // TODO: Multi-targeting
                    return $"{displayName} ({projectConfiguration.Name})";
                }
            }
        }
    }
}
