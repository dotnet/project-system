using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemCommandSet, ManagedProjectSystemPackage.EditProjectFileCmdId)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class EditCsprojCommand : AbstractEditProjectFileCommand
    {
        [ImportingConstructor]
        public EditCsprojCommand(UnconfiguredProject unconfiguredProject,
            IProjectCapabilitiesService projectCapabilitiesService,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IMsBuildAccessor msbuildAccessor,
            IFileSystem fileSystem,
            ITextDocumentFactoryService textDocumentService,
            IVsEditorAdaptersFactoryService editorFactoryService,
            IProjectThreadingService threadingService,
            IVsShellUtilitiesHelper shellHelper,
            IExportFactory<IMsBuildModelWatcher> watcherFactory) :
            base(unconfiguredProject, projectCapabilitiesService, serviceProvider, msbuildAccessor, fileSystem,
                textDocumentService, editorFactoryService, threadingService, shellHelper, watcherFactory)
        {
        }

        protected override string FileExtension { get; } = "csproj";
    }
}
