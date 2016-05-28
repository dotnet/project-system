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
                
                if (!fileNode.Flags.IsIncludedInProject())
                {
                    return null;
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

        private string FindFile(string specialFileName)
        {
            IProjectTree rootNode = ProjectTreeService.CurrentTree.Tree;

            if (specialFileName == null)
            {
                return null;
            }

            string specialFilePath;
            // First, we look in the AppDesigner folder.
            IProjectTree appDesignerFolder = rootNode.Children.FirstOrDefault(child => child.IsFolder && child.Flags.HasFlag(ProjectTreeFlags.Common.AppDesignerFolder));
            if (appDesignerFolder != null && CreatedByDefaultUnderAppDesignerFolder)
            {
                specialFilePath = FindFileWithinNode(appDesignerFolder, specialFileName);

                if (specialFilePath != null)
                {
                    return specialFilePath;
                }
            }

            // Now try the root folder.
            specialFilePath = FindFileWithinNode(rootNode, specialFileName);
            if (specialFilePath != null)
            {
                return specialFilePath;
            }

            return null;
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
                        return specialFilePath;
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
        public Task<string> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags, CancellationToken cancellationToken = default(CancellationToken))
        {
            string specialFileName = GetFileNameOfSpecialFile(fileId);

            // Search for the file in the app designer and root folders.
            string specialFilePath = FindFile(specialFileName);
            if (specialFilePath != null)
            {
                return Task.FromResult(specialFilePath);
            }

            // File doesn't exist. Create it if we've been asked to.
            if (flags.HasFlag(SpecialFileFlags.CreateIfNotExist))
            {
                specialFilePath = CreateFile(fileId, specialFileName);
                if (specialFilePath != null)
                {
                    return Task.FromResult(specialFilePath);
                }
            }

            // We haven't found the file but return the default file path as that's the contract.
            string rootFilePath = ProjectVsServices.ActiveConfiguredProject.UnconfiguredProject.FullPath; // PhysicalProjectTreeProvider.Value.GetPath(rootNode);
            string fullPath = Path.Combine(rootFilePath, specialFileName);
            return Task.FromResult(fullPath);
        }
    }
}
