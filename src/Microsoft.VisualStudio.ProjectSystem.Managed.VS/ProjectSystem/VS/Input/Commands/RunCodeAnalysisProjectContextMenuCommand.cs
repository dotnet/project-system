// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(ManagedProjectSystemPackage.VSStd2KCommandSet, ManagedProjectSystemPackage.RunCodeAnalysisProjectContextMenuCmdId)]
    [AppliesTo(ProjectCapability.CodeAnalysis)]
    internal class RunCodeAnalysisProjectContextMenuCommand : AbstractRunCodeAnalysisCommand
    {
        [ImportingConstructor]
        public RunCodeAnalysisProjectContextMenuCommand(
            UnconfiguredProject unconfiguredProject,
            IProjectThreadingService threadingService,
            SVsServiceProvider serviceProvider,
            RunCodeAnalysisBuildPropertyProvider runCodeAnalysisBuildPropertyProvider)
            : base(unconfiguredProject, threadingService, serviceProvider, runCodeAnalysisBuildPropertyProvider)
        {
        }

        protected override bool ShouldHandle(IProjectTree node) => node.IsRoot();
        protected override string GetCommandText() => VSResources.RunCodeAnalysisProjectContextMenuCommand;
        protected override long CommandId => ManagedProjectSystemPackage.RunCodeAnalysisProjectContextMenuCmdId;
    }
}
