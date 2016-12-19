// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemCommandSet, ManagedProjectSystemPackage.GenerateNuGetPackageTopLevelBuildCmdId)]
    //[AppliesTo(ProjectCapability.GenerateNuGetPackage)]
    internal class GenerateNuGetPackageTopLevelBuildMenuCommand : AbstractGenerateNuGetPackageCommand
    {
        [ImportingConstructor]
        public GenerateNuGetPackageTopLevelBuildMenuCommand(UnconfiguredProject unconfiguredProject, IProjectThreadingService threadingService)
            : base (unconfiguredProject, threadingService)
        {
        }

        protected override bool ShouldHandle(IProjectTree node) => true;
        protected override string GetCommandText() =>
            string.Format(VSResources.PackSelectedProjectCommand, Path.GetFileNameWithoutExtension(UnconfiguredProject.FullPath));
    }
}
