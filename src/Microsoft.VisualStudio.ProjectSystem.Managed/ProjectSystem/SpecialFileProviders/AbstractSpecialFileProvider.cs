using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectSystem.SpecialFileProviders
{
    internal abstract class AbstractSpecialFileProvider : ISpecialFileProvider
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

        protected virtual bool ShouldLookInAppDesignerFolder => true;

        protected abstract string GetFileNameOfSpecialFile(SpecialFiles fileId);

        protected abstract string GetTemplateForSpecialFile(SpecialFiles fileId);

        private string FindFileWithinNode(IProjectTree parentNode, string fileName)
        {
            IProjectTree fileNode;
            parentNode.TryFindImmediateChild(fileName, out fileNode);

            if (fileNode != null)
            {
                // The user has created a folder with this name which means we don't have a special file.
                if (fileNode.IsFolder)
                {
                    return null;
                }

                if (fileNode.Flags.HasFlag(ProjectTreeFlags.Common.Linked))
                {
                    // TODO
                    // parentNodeFilePath = Path.Get(fileNode.FilePath);
                }

                // TODO : if in project check?
                // TODO: AssureLocalCheck?
                if (!File.Exists(fileNode.FilePath))
                {
                    return null;
                }

                return fileNode.FilePath;
            }

            return null;
        }

        /// <summary>
        /// We follow this algorithm for looking up files:
        ///
        /// if (not asked to create)
        ///      Look in AppDesigner folder
        ///      Look in root folder
        ///
        /// if (asked to create)
        ///      Look in AppDesigner folder
        ///      Look in root folder
        ///      Force-create in app-designer folder unless it's the app.config file, which
        ///          the users expect in the root folder.
        /// </summary>
        public Task<string> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags, CancellationToken cancellationToken = default(CancellationToken))
        {
            IProjectTree rootNode = ProjectTreeService.CurrentTree.Tree;
            
            string specialFileName = GetFileNameOfSpecialFile(fileId);

            if (specialFileName == null)
            { 
                return null;
            }

            string specialFilePath;
            // First, we look in the AppDesigner folder.
            IProjectTree appDesignerFolder = rootNode.Children.FirstOrDefault(child => child.IsFolder && child.Flags.HasFlag(ProjectTreeFlags.Common.AppDesignerFolder));
            if (appDesignerFolder != null && ShouldLookInAppDesignerFolder)
            {
                specialFilePath = FindFileWithinNode(appDesignerFolder, specialFileName);

                if (specialFilePath != null)
                {
                    return Task.FromResult(specialFilePath);
                }
            }

            // Now try the root folder.
            specialFilePath = FindFileWithinNode(rootNode, specialFileName);
            if (specialFilePath != null)
            {
                return Task.FromResult(specialFilePath);
            }

            // File doesn't exist. Create it if we've been asked to.
            if (flags.HasFlag(SpecialFileFlags.CreateIfNotExist))
            {
                string templateFile = GetTemplateForSpecialFile(fileId);

                // Create file.
                if (templateFile == null)
                {

                }
            }

            // We haven't found the file but return the default file path as that's the contract.
            string rootFilePath = PhysicalProjectTreeProvider.Value.GetPath(rootNode);
            string fullPath = Path.Combine(rootFilePath, specialFileName);
            return Task.FromResult(fullPath);
        }
    }
}
