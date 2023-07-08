// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the AppDesigner folder;
    ///     called "Properties" in C# and "My Project" in Visual Basic.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AppDesigner)]
    [Export(typeof(IAppDesignerFolderSpecialFileProvider))]
    [AppliesTo(ProjectCapability.AppDesigner)]
    internal class AppDesignerFolderSpecialFileProvider : AbstractSpecialFileProvider, IAppDesignerFolderSpecialFileProvider
    {
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public AppDesignerFolderSpecialFileProvider(IPhysicalProjectTree projectTree, ProjectProperties properties)
            : base(projectTree, isFolder: true)
        {
            _properties = properties;
        }

        protected override async Task<IProjectTree?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            // First look for the actual AppDesigner folder
            IProjectTree? folder = FindAppDesignerFolder(root);
            if (folder is null)
            {
                // Otherwise, find a location that is a candidate
                folder = await FindAppDesignerFolderCandidateAsync(provider, root);
            }

            return folder;
        }

        protected override async Task<string?> GetDefaultFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            string? projectPath = provider.GetRootedAddNewItemDirectory(root);
            if (projectPath is null)  // Root has DisableAddItem
                return null;

            string? folderName = await GetDefaultAppDesignerFolderNameAsync();
            if (string.IsNullOrEmpty(folderName))
                return null; // Developer has set the AppDesigner path to empty

            return Path.Combine(projectPath, folderName);
        }

        private async Task<string?> GetDefaultAppDesignerFolderNameAsync()
        {
            AppDesigner general = await _properties.GetAppDesignerPropertiesAsync();

            return (string?)await general.FolderName.GetValueAsync();
        }

        private static IProjectTree? FindAppDesignerFolder(IProjectTree root)
        {
            return root.GetSelfAndDescendentsBreadthFirst()
                       .FirstOrDefault(child => child.Flags.HasFlag(ProjectTreeFlags.Common.AppDesignerFolder));
        }

        private async Task<IProjectTree?> FindAppDesignerFolderCandidateAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            string? path = await GetDefaultFileAsync(provider, root);
            if (path is null)
                return null;

            return provider.FindByPath(root, path);
        }
    }
}
