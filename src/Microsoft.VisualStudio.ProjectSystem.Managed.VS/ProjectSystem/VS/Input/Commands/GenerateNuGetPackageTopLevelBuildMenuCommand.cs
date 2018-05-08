// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.IO;

using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemCommandSet, ManagedProjectSystemPackage.GenerateNuGetPackageTopLevelBuildCmdId)]
    [AppliesTo(ProjectCapability.Pack)]
    internal class GenerateNuGetPackageTopLevelBuildMenuCommand : AbstractGenerateNuGetPackageCommand
    {
        [ImportingConstructor]
        public GenerateNuGetPackageTopLevelBuildMenuCommand(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            SVsServiceProvider serviceProvider,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
            : base(project, threadingService, serviceProvider, generatePackageOnBuildPropertyProvider)
        {
        }

        protected override bool ShouldHandle(IProjectTree node) => true;
        protected override string GetCommandText() =>
            string.Format(VSResources.PackSelectedProjectCommand, Path.GetFileNameWithoutExtension(Project.FullPath));
    }
}
