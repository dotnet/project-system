// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(CommandGroup.ManagedProjectSystem, ManagedProjectSystemCommandId.GenerateNuGetPackageTopLevelBuild)]
    [AppliesTo(ProjectCapability.Pack)]
    internal class GenerateNuGetPackageTopLevelBuildMenuCommand : AbstractGenerateNuGetPackageCommand
    {
        [ImportingConstructor]
        public GenerateNuGetPackageTopLevelBuildMenuCommand(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> vsSolutionBuildManagerService,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
            : base(project, threadingService, vsSolutionBuildManagerService, generatePackageOnBuildPropertyProvider)
        {
        }

        protected override bool ShouldHandle(IProjectTree node) => true;

        protected override string GetCommandText() =>
            string.Format(VSResources.PackSelectedProjectCommand, Path.GetFileNameWithoutExtension(Project.FullPath));
    }
}
