// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides <see langword="abstract"/> base class for <see cref="ISpecialFileProvider"/> instances 
    ///     that find their special file by file name under the AppDesigner folder, falling back 
    ///     to the root folder if it doesn't exist.
    /// </summary>
    internal class AbstractFindByNameUnderAppDesignerSpecialFileProvider : AbstractFindByNameSpecialFileProvider
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
            if (appDesignerFolder != null)
            {
                IProjectTree? node = await base.FindFileAsync(provider, appDesignerFolder);
                if (node != null)
                    return node;
            }

            // Then fallback to project root
            return await base.FindFileAsync(provider, root);
        }

        protected override async Task<string?> GetDefaultFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            // AppDesigner folder first if it exists
            string? appDesignerPath = await GetAppDesignerFolderPathAsync();
            if (appDesignerPath != null)
            {
                return GetDefaultFileAsync(appDesignerPath);
            }

            // Then fallback to project root
            return await base.GetDefaultFileAsync(provider, root);
        }

        private async Task<IProjectTree?> GetAppDesignerFolderAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            string? appDesignerPath = await GetAppDesignerFolderPathAsync();
            if (appDesignerPath == null)
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
            await EnsureAppDesignerFolder();

            await CreateFileCoreAsync(path);
        }

        protected virtual Task CreateFileCoreAsync(string path)
        {
            return base.CreateFileAsync(path);
        }

        private Task EnsureAppDesignerFolder()
        {
            return GetAppDesignerFolderPathAsync(createIfNotExists: true);
        }
    }
}
