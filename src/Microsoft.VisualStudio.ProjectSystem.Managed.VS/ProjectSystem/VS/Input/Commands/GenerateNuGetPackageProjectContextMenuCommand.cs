// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell.Interop;

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
            IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> vsSolutionBuildManagerService,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
            : base(project, threadingService, vsSolutionBuildManagerService, generatePackageOnBuildPropertyProvider)
        {
        }

        protected override bool ShouldHandle(IProjectTree node) => node.IsRoot();

        protected override string GetCommandText() => VSResources.PackCommand;
    }
}
