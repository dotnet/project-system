// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the default 'Settings.settings' file;
    ///     which contains applications settings for a project and is typically found under the 'AppDesigner'
    ///     folder.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AppSettings)]
    [AppliesTo(ProjectCapability.DotNet + " & " + ProjectCapability.AppSettings)]
    internal class AppSettingsSpecialFileProvider : AbstractFindByNameUnderAppDesignerSpecialFileProvider
    {
        private readonly ICreateFileFromTemplateService _templateFileCreationService;

        [ImportingConstructor]
        public AppSettingsSpecialFileProvider(
            ISpecialFilesManager specialFilesManager,
            IPhysicalProjectTree projectTree,
            ICreateFileFromTemplateService templateFileCreationService)
            : base("Settings.settings", specialFilesManager, projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
        }

        protected override Task CreateFileCoreAsync(string path)
        {
            return _templateFileCreationService.CreateFileAsync("SettingsInternal.zip", path);
        }
    }
}
