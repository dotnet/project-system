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
    [ExportSpecialFileProvider(SpecialFiles.AppSettings)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    public class SettingsFileSpecialFileProvider : ISpecialFileProvider
    {
        /// <summary>
        /// Gets the physical tree provider.
        /// </summary>
        [Import(ExportContractNames.ProjectTreeProviders.PhysicalViewTree)]
        private Lazy<IProjectTreeProvider> PhysicalProjectTreeProvider { get; set; }

        /// <summary>
        /// Gets or sets the project tree service.
        /// </summary>
        [Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]
        private IProjectTreeService ProjectTreeService { get; set; }

        /// <summary>
        /// Gets or sets the accessor to project items.
        /// </summary>
        [Import(ExportContractNames.ProjectItemProviders.Folders)]
        private Lazy<IProjectItemProvider> Folders { get; set; }

        /// <summary>
        /// Gets or sets the accessor to project items.
        /// </summary>
        [Import(ExportContractNames.ProjectItemProviders.SourceFiles)]
        private Lazy<IProjectItemProvider> SourceItems { get; set; }

        public Task<string> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags, CancellationToken cancellationToken = default(CancellationToken))
        {
            Assert(fileId == SpecialFiles.AppSettings);

            IProjectTree appDesignerFolder = ProjectTreeService.CurrentTree.Tree.Children.FirstOrDefault(child => child.IsFolder && child.Flags.HasFlag(ProjectTreeFlags.Common.AppDesignerFolder));
            if (appDesignerFolder != null)
            {
                IProjectTree settingsNode;
                appDesignerFolder.TryFindImmediateChild("Settings.settings", out settingsNode);

                return Task.FromResult(settingsNode?.FilePath);
            }

            //string rootDir = Path.GetDirectoryName(this.ProjectTreeService.CurrentTree.ProjectSnapshot.Value.FullPath);
            //string settingsPath = Path.Combine(rootDir, "Settings.settings");
            //return settingsPath;

            return null;
        }
    }
}
