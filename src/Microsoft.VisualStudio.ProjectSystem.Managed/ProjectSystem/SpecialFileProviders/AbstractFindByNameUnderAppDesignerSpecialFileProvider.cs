// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides <see langword="abstract"/> base class for <see cref="ISpecialFileProvider"/> instances
    ///     that find their special file by file name under the AppDesigner folder, falling back
    ///     to the root folder if it doesn't exist.
    /// </summary>
    internal abstract class AbstractFindByNameUnderAppDesignerSpecialFileProvider : AbstractFindByNameSpecialFileProvider
    {
        private readonly ISpecialFilesManager _specialFilesManager;

        protected AbstractFindByNameUnderAppDesignerSpecialFileProvider(string fileName, ISpecialFilesManager specialFilesManager, IPhysicalProjectTree projectTree)
            : base(fileName, projectTree)
        {
            _specialFilesManager = specialFilesManager;
        }

        protected override async Task<IProjectTree?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            // Search AppDesigner folder first if it exists
            IProjectTree? appDesignerFolder = await GetAppDesignerFolderAsync(provider, root);
            if (appDesignerFolder is not null)
            {
                IProjectTree? node = await base.FindFileAsync(provider, appDesignerFolder);
                if (node is not null)
                    return node;
            }

            // Then fallback to project root
            return await base.FindFileAsync(provider, root);
        }

        protected override async Task<string?> GetDefaultFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            // AppDesigner folder first if it exists
            string? appDesignerPath = await GetAppDesignerFolderPathAsync();
            if (appDesignerPath is not null)
            {
                return GetDefaultFile(appDesignerPath);
            }

            // Then fallback to project root
            return await base.GetDefaultFileAsync(provider, root);
        }

        private async Task<IProjectTree?> GetAppDesignerFolderAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            string? appDesignerPath = await GetAppDesignerFolderPathAsync();
            if (appDesignerPath is null)
                return null;

            return provider.FindByPath(root, appDesignerPath);
        }

        private Task<string?> GetAppDesignerFolderPathAsync(bool createIfNotExists = false)
        {
            SpecialFileFlags flags = SpecialFileFlags.FullPath;
            if (createIfNotExists)
                flags |= SpecialFileFlags.CreateIfNotExist;

            return _specialFilesManager.GetFileAsync(SpecialFiles.AppDesigner, flags);
        }

        protected sealed override async Task CreateFileAsync(string path)
        {
            await EnsureAppDesignerFolderAsync();

            await CreateFileCoreAsync(path);
        }

        protected virtual Task CreateFileCoreAsync(string path)
        {
            return base.CreateFileAsync(path);
        }

        private Task EnsureAppDesignerFolderAsync()
        {
            return GetAppDesignerFolderPathAsync(createIfNotExists: true);
        }
    }
}
