// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

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
