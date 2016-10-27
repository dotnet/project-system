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
    [AppliesTo(ProjectCapability.CSharp)]
    internal class EditCsprojCommand : AbstractEditProjectFileCommand
    {
        [ImportingConstructor]
        public EditCsprojCommand(IUnconfiguredProjectVsServices projectVsServices,
            IProjectCapabilitiesService projectCapabilitiesService,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IProjectLockService lockService,
            IFileSystem fileSystem,
            ITextDocumentFactoryService textDocumentService,
            IVsEditorAdaptersFactoryService editorFactoryService) :
            base(projectVsServices, projectCapabilitiesService, serviceProvider, lockService, fileSystem, textDocumentService, editorFactoryService)
        {
        }

        protected override string FileExtension { get; } = "csproj";
    }
}
