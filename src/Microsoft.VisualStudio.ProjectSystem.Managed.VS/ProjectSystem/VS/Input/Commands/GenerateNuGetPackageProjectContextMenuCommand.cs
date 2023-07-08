// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.Build;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(CommandGroup.ManagedProjectSystem, ManagedProjectSystemCommandId.GenerateNuGetPackageProjectContextMenu)]
    [AppliesTo(ProjectCapability.Pack)]
    internal class GenerateNuGetPackageProjectContextMenuCommand : AbstractGenerateNuGetPackageCommand
    {
        [ImportingConstructor]
        public GenerateNuGetPackageProjectContextMenuCommand(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            ISolutionBuildManager vsSolutionBuildManagerService,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
            : base(project, threadingService, vsSolutionBuildManagerService, generatePackageOnBuildPropertyProvider)
        {
        }

        protected override bool ShouldHandle(IProjectTree node) => node.IsRoot();

        protected override string GetCommandText() => VSResources.PackCommand;
    }
}
