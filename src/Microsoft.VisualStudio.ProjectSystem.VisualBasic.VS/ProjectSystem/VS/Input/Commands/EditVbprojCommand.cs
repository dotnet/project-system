// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(ManagedProjectSystemPackage.DotnetProjectSystemCommandSet, ManagedProjectSystemPackage.EditProjectFileCmdId)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class EditVbprojCommand : AbstractEditProjectFileCommand
    {
        [ImportingConstructor]
        public EditVbprojCommand(IUnconfiguredProjectVsServices projectVsServices,
            IProjectCapabilitiesService projectCapabilitiesService,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider) :
            base(projectVsServices, projectCapabilitiesService, serviceProvider)
        {
        }

        protected override string GetCommandText(IProjectTree node)
        {
            return string.Format(VSVisualBasicResources.EditVbprojCommand, node.Caption);
        }
    }
}
