// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the AppDesigner folder;
    ///     called "Properties" in C# and "My Project" in Visual Basic.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AppDesigner)]
    [Export(typeof(AppDesignerFolderSpecialFileProvider))]
    [Export(typeof(IAppDesignerFolderSpecialFileProvider))]
    [AppliesTo(ProjectCapability.AppDesigner)]
    internal class AppDesignerFolderSpecialFileProvider : AbstractSpecialFileProvider, IAppDesignerFolderSpecialFileProvider
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public AppDesignerFolderSpecialFileProvider(IPhysicalProjectTree projectTree, ProjectProperties properties)
            : base(projectTree.TreeService)
        {
            _projectTree = projectTree;
            _properties = properties;
        }

        protected override Task<string?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            IProjectTree? folder = root?.GetSelfAndDescendentsBreadthFirst().FirstOrDefault(child => child.Flags.HasFlag(ProjectTreeFlags.Common.AppDesignerFolder));

            if (folder == null)
                return Task.FromResult<string?>(null);

            return Task.FromResult(provider.GetRootedAddNewItemDirectory(folder));
        }

        protected override Task CreateFileAsync(string path)
        {
            return _projectTree.TreeStorage.CreateFolderAsync(path);
        }

        protected override async Task<string?> GetDefaultFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            string? rootPath = provider.GetRootedAddNewItemDirectory(root);

            Assumes.NotNull(rootPath);

            string folderName = await GetDefaultAppDesignerFolderNameAsync();
            if (string.IsNullOrEmpty(folderName))
                return null; // Developer has set the AppDesigner path to empty

            return Path.Combine(rootPath, folderName);
        }

        private async Task<string> GetDefaultAppDesignerFolderNameAsync()
        {
            AppDesigner general = await _properties.GetAppDesignerPropertiesAsync();

            return (string)await general.FolderName.GetValueAsync();
        }
    }
}
