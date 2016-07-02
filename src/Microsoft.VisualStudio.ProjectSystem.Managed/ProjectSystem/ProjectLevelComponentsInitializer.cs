// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// Unconfigured project scope component responsible for initializing early other 
    /// components on an unconfigured project scope. 
    /// </summary>
    internal class ProjectLevelComponentsInitializer
    {
        private IUnconfiguredProjectCommonServices ProjectCommonServices { get; set; }

        [ImportingConstructor]
        public ProjectLevelComponentsInitializer(IUnconfiguredProjectCommonServices projectVsServices)
        {
            ProjectCommonServices = projectVsServices;
        }

        [ProjectAutoLoad(completeBy: ProjectLoadCheckpoint.BeforeLoadInitialConfiguration)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        private Task Initialize()
        {
            // Hook into the unload event so we can unload the in memory project
            ProjectCommonServices.Project.ProjectUnloading += UnconfiguredProject_ProjectUnloading;
            ActiveProjectTracker.Instance.Add(ProjectCommonServices.Project);

            return TplExtensions.CompletedTask;
        }

        /// <summary>
        /// Event handler for when the project unloads. Use it to clean up
        /// </summary>
        private Task UnconfiguredProject_ProjectUnloading(object sender, EventArgs args)
        {
            return ProjectCommonServices.ThreadingService.JoinableTaskFactory.RunAsync(() =>
            {
                ProjectCommonServices.Project.ProjectUnloading -= UnconfiguredProject_ProjectUnloading;
                
                ActiveProjectTracker.Instance.RemoveReturnCount(ProjectCommonServices.Project);

                return TplExtensions.CompletedTask;
            }).Task;
        }
    }
}
