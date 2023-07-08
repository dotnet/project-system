// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the default 'app.manifest' file;
    ///     which contains Win32 directives for assembly binding, compatibility and elevation and is
    ///     typically found under the 'AppDesigner' folder.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AppManifest)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class AppManifestSpecialFileProvider : AbstractFindByNameUnderAppDesignerSpecialFileProvider
    {
        private readonly ProjectProperties _properties;
        private readonly ICreateFileFromTemplateService _templateFileCreationService;

        [ImportingConstructor]
        public AppManifestSpecialFileProvider(
            ISpecialFilesManager specialFilesManager,
            IPhysicalProjectTree projectTree,
            ICreateFileFromTemplateService templateFileCreationService,
            ProjectProperties properties)
            : base("app.manifest", specialFilesManager, projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
            _properties = properties;
        }

        protected override async Task CreateFileCoreAsync(string path)
        {
            await _templateFileCreationService.CreateFileAsync("AppManifestInternal.zip", path);
        }

        protected override async Task<IProjectTree?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            string? path = await GetAppManifestPathFromPropertiesAsync();
            if (path is null)
                return await base.FindFileAsync(provider, root);

            return provider.FindByPath(root, path);
        }

        private async Task<string?> GetAppManifestPathFromPropertiesAsync()
        {
            ConfigurationGeneralBrowseObject configurationGeneral = await _properties.GetConfigurationGeneralBrowseObjectPropertiesAsync();

            string? value = (string?)await configurationGeneral.ApplicationManifest.GetValueAsync();
            if (Strings.IsNullOrEmpty(value))
                return null;

            if (StringComparers.PropertyLiteralValues.Equals(value, "DefaultManifest"))
                return null;

            if (StringComparers.PropertyLiteralValues.Equals(value, "NoManifest"))
                return null;

            return value;
        }
    }
}
