// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AppManifest)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class AppManifestSpecialFileProvider : AbstractFindByNameUnderAppDesignerSpecialFileProvider
    {
        private readonly ProjectProperties _properties;
        private readonly ICreateFileFromTemplateService _templateFileCreationService;
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public AppManifestSpecialFileProvider(
            ISpecialFilesManager specialFilesManager,
            IPhysicalProjectTree projectTree,            
            ICreateFileFromTemplateService templateFileCreationService, 
            UnconfiguredProject project,
            ProjectProperties properties)
            : base("app.manifest", specialFilesManager, projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
            _project = project;
            _properties = properties;
        }

        protected override Task CreateFileAsync(string path)
        {
            return _templateFileCreationService.CreateFileAsync("ResourceInternal.zip", path);
        }

        protected override async Task<IProjectTree?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            string? path = await GetAppManifestPathFromPropertiesAsync();
            if (path == null)
                return await base.FindFileAsync(provider, root);

            return provider.FindByPath(root, path);
        }

        private async Task<string?> GetAppManifestPathFromPropertiesAsync()
        {
            ConfigurationGeneralBrowseObject configurationGeneral = await _properties.GetConfigurationGeneralBrowseObjectPropertiesAsync();

            string value = (string)await configurationGeneral.ApplicationManifest.GetValueAsync();
            if (value.Length == 0)
                return null;

            if (StringComparers.PropertyLiteralValues.Equals(value, "DefaultManifest"))
                return null;

            if (StringComparers.PropertyLiteralValues.Equals(value, "NoManifest"))
                return null;

            return value;
        }
    }
}
