// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal class TargetedProjectContext : ITargetedProjectContext
    {
        public TargetedProjectContext(
            ITargetFramework targetFramework, 
            string projectFilePath, 
            string displayName, 
            string targetPath)
        {
            TargetFramework = targetFramework;
            ProjectFilePath = projectFilePath;
            DisplayName = displayName;
            TargetPath = targetPath;
        }

        public string DisplayName { get; set; }
        public string ProjectFilePath { get; set; }
        public ITargetFramework TargetFramework { get; }
        public string TargetPath { get; }
        public bool LastDesignTimeBuildSucceeded { get; set; }

        public void Dispose()
        {
        }
    }
}
