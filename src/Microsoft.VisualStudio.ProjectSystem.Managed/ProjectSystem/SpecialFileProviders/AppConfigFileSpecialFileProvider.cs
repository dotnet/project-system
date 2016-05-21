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
    [ExportSpecialFileProvider(SpecialFiles.AppConfig)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class AppConfigFileSpecialFileProvider : ISpecialFileProvider
    {
        /// <summary>
        /// Gets or sets the project tree service.
        /// </summary>
        [Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]
        private IProjectTreeService ProjectTreeService { get; set; }

        public Task<string> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags, CancellationToken cancellationToken = default(CancellationToken))
        {
            Assert(fileId == SpecialFiles.AppConfig);
            
            IProjectTree settingsNode;
            ProjectTreeService.CurrentTree.Tree.TryFindImmediateChild("App.config", out settingsNode);

            return Task.FromResult(settingsNode?.FilePath);

            //string rootDir = Path.GetDirectoryName(this.ProjectTreeService.CurrentTree.ProjectSnapshot.Value.FullPath);
            //string settingsPath = Path.Combine(rootDir, "Settings.settings");
            //return settingsPath;
        }
    }
}
