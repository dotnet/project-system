// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemCommandSet, ManagedProjectSystemPackage.GenerateNuGetPackageProjectContextMenuCmdId)]
    [AppliesTo(ProjectCapability.Pack)]
    internal class GenerateNuGetPackageProjectContextMenuCommand : AbstractGenerateNuGetPackageCommand
    {
        [ImportingConstructor]
        public GenerateNuGetPackageProjectContextMenuCommand(
            UnconfiguredProject unconfiguredProject,
            IProjectThreadingService threadingService,
            SVsServiceProvider serviceProvider,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
            : base(unconfiguredProject, threadingService, serviceProvider, generatePackageOnBuildPropertyProvider)
        {
        }

        protected override bool ShouldHandle(IProjectTree node) => node.IsRoot();
        protected override string GetCommandText() => VSResources.PackCommand;
    }
}
