// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
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
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IProjectLockService lockService,
            IFileSystem fileSystem,
            ITextDocumentFactoryService textDocumentService,
            IVsEditorAdaptersFactoryService editorFactoryService) :
            base(projectVsServices, projectCapabilitiesService, serviceProvider, lockService, fileSystem, textDocumentService, editorFactoryService)
        {
        }

        protected override string FileExtension { get; } = "vbproj";
    }
}
