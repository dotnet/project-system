using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using Diagnostics = System.Diagnostics;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.SpecialFileProviders
{
    internal abstract class AbstractSpecialFileProvider : ISpecialFileProvider
    {
        /// <summary>
        /// Gets or sets the project tree service.
        /// </summary>
        [Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]
        private IProjectTreeService ProjectTreeService { get; set; }

        [Import]
        private IUnconfiguredProjectVsServices ProjectVsServices { get; set; }

        protected virtual bool CreatedByDefaultUnderAppDesignerFolder => true;

        protected abstract string GetFileNameOfSpecialFile(SpecialFiles fileId);

        protected abstract string GetTemplateForSpecialFile(SpecialFiles fileId);

        /// <summary>
        /// Find a file with the given filename within given the node.
        /// </summary>
        private IProjectTree FindFileWithinNode(IProjectTree parentNode, string fileName)
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
                
                return fileNode;
            }

            return null;
        }

        /// <summary>
        /// Find a file with the given file name. The algorithm used is :
        ///       Look under the appdesigner folder for files that normally live there.
        ///       Look under the project root for all files.
        /// </summary>
        private IProjectTree FindFile(string specialFileName)
        {
            IProjectTree rootNode = ProjectTreeService.CurrentTree.Tree;

            if (specialFileName == null)
            {
                return null;
            }

            IProjectTree specialFileNode;
            // First, we look in the AppDesigner folder.
            IProjectTree appDesignerFolder = rootNode.Children.FirstOrDefault(child => child.IsFolder && child.Flags.HasFlag(ProjectTreeFlags.Common.AppDesignerFolder));
            if (appDesignerFolder != null && CreatedByDefaultUnderAppDesignerFolder)
            {
                specialFileNode = FindFileWithinNode(appDesignerFolder, specialFileName);

                if (specialFileNode != null)
                {
                    return specialFileNode;
                }
            }

            // Now try the root folder.
            specialFileNode = FindFileWithinNode(rootNode, specialFileName);
            if (specialFileNode != null)
            {
                return specialFileNode;
            }

            return null;
        }

        /// <summary>
        /// Check to see if a given node is both part of a project and exists on disk.
        /// </summary>
        private async Task<bool> IsNodeInSyncWithDiskAsync(IProjectTree specialFileNode, bool forceSync, CancellationToken cancellationToken)
        {
            if (!specialFileNode.Flags.IsIncludedInProject())
            {
                if (forceSync)
                {
                    var showAllFilesProvider = ProjectTreeService.CurrentTree.TreeProvider as IShowAllFilesProjectTreeProvider;

                    // Cannot include files if tree provider doesn't support it.
                    if (showAllFilesProvider == null)
                    {
                        return false;
                    }

                    await showAllFilesProvider.IncludeItemsAsync(ImmutableHashSet.Create(specialFileNode));
                    await ProjectTreeService.PublishLatestTreeAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    return false;
                }
            }

            // TODO: AssureLocalCheck?
            if (!File.Exists(specialFileNode.FilePath))
            {
                if (forceSync)
                {
                    await ProjectTreeService.CurrentTree.TreeProvider.RemoveAsync(ImmutableHashSet.Create(specialFileNode));
                    await ProjectTreeService.PublishLatestTreeAsync(cancellationToken: cancellationToken);
                }

                return false;
            }

            return true;
        }

        private string GetTemplateLanguage(Project project)
        {
            switch (project.CodeModel.Language)
            {
                case CodeModelLanguageConstants.vsCMLanguageCSharp:
                    return "CSharp";
                case CodeModelLanguageConstants.vsCMLanguageVB:
                    return "VisualBasic";
                default:
                    throw new NotSupportedException("Unrecognized language");
            }
        }

        private string CreateFile(SpecialFiles fileId, string specialFileName)
        {
            string templateFile = GetTemplateForSpecialFile(fileId);

            Project project = ProjectVsServices.Hierarchy.GetProperty<EnvDTE.Project>(Shell.VsHierarchyPropID.ExtObject, null);
            var solution = project.DTE.Solution as Solution2;

            string templateFilePath = solution.GetProjectItemTemplate(templateFile, GetTemplateLanguage(project));

            // Create file.
            if (templateFilePath != null)
            {
                IProjectTree rootNode = ProjectTreeService.CurrentTree.Tree;
                IProjectTree appDesignerFolder = rootNode.Children.FirstOrDefault(child => child.IsFolder && child.Flags.HasFlag(ProjectTreeFlags.Common.AppDesignerFolder));

                if (appDesignerFolder == null && CreatedByDefaultUnderAppDesignerFolder)
                {
                    return null;
                }

                var parentId = CreatedByDefaultUnderAppDesignerFolder ? (uint)(appDesignerFolder.Identity) : (uint)VSConstants.VSITEMID.Root;
                var result = new VSADDRESULT[1];
                ProjectVsServices.Project.AddItemWithSpecific(parentId, VSADDITEMOPERATION.VSADDITEMOP_RUNWIZARD, specialFileName, 0, new string[] { templateFilePath }, IntPtr.Zero, 0, Guid.Empty, null, Guid.Empty, result);

                if (result[0] == VSADDRESULT.ADDRESULT_Success)
                {
                    // The tree would have changed. So fetch the nodes again from the current tree.
                    var specialFilePath = FindFile(specialFileName);

                    if (specialFilePath != null)
                    {
                        return specialFilePath.FilePath;
                    }
                    Diagnostics.Debug.Fail("We added the file successfully but didn't find it!");
                }
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
        public async Task<string> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags, CancellationToken cancellationToken = default(CancellationToken))
        {
            string specialFileName = GetFileNameOfSpecialFile(fileId);

            // Search for the file in the app designer and root folders.
            IProjectTree specialFileNode = FindFile(specialFileName);
            if (specialFileNode != null)
            {
                if (await IsNodeInSyncWithDiskAsync(specialFileNode, forceSync: flags.HasFlag(SpecialFileFlags.CreateIfNotExist), cancellationToken: cancellationToken))
                {
                    return specialFileNode.FilePath;
                }
            }

            // File doesn't exist. Create it if we've been asked to.
            if (flags.HasFlag(SpecialFileFlags.CreateIfNotExist))
            {
                string createdFilePath = CreateFile(fileId, specialFileName);
                if (createdFilePath != null)
                {
                    return createdFilePath;
                }
            }

            // We haven't found the file but return the default file path as that's the contract.
            string rootFilePath = ProjectVsServices.ActiveConfiguredProject.UnconfiguredProject.FullPath; // PhysicalProjectTreeProvider.Value.GetPath(rootNode);
            string fullPath = Path.Combine(rootFilePath, specialFileName);
            return fullPath;
        }
    }
}
