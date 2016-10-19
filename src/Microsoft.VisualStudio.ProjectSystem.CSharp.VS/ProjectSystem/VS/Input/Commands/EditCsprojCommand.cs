using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
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
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider) :
            base(projectVsServices, projectCapabilitiesService, serviceProvider)
        {
        }

        protected override string GetCommandText(IProjectTree node)
        {
            return string.Format(VSCSharpResources.EditCsprojCommand, node.Caption);
        }
    }
}
