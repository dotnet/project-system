using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AssemblyResource)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ResourcesFileSpecialFileProvider : ISpecialFileProvider
    {
        /// <summary>
        /// Gets or sets the project tree service.
        /// </summary>
        [Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]
        private IProjectTreeService ProjectTreeService { get; set; }

        public Task<string> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags, CancellationToken cancellationToken = default(CancellationToken))
        {
            Assert(fileId == SpecialFiles.AssemblyResource);

            IProjectTree appDesignerFolder = ProjectTreeService.CurrentTree.Tree.Children.FirstOrDefault(child => child.IsFolder && child.Flags.HasFlag(ProjectTreeFlags.Common.AppDesignerFolder));
            if (appDesignerFolder != null)
            {
                IProjectTree resourcesNode;
                appDesignerFolder.TryFindImmediateChild("Resources.resx", out resourcesNode);

                return Task.FromResult(resourcesNode?.FilePath);
            }

            //string rootDir = Path.GetDirectoryName(this.ProjectTreeService.CurrentTree.ProjectSnapshot.Value.FullPath);
            //string settingsPath = Path.Combine(rootDir, "Settings.settings");
            //return settingsPath;

            return null;
        }
    }
}
