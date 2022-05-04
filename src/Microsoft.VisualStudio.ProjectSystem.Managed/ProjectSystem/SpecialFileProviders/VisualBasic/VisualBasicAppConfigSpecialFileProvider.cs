// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders.VisualBasic
{
    /// <summary>
    /// Provides a <see cref="ISpecialFileProvider"/> that handles the default 'App.config' file.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AppConfig)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicAppConfigSpecialFileProvider : AbstractFindByNameSpecialFileProvider
    {
        private readonly ICreateFileFromTemplateService _templateFileCreationService;
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public VisualBasicAppConfigSpecialFileProvider(IPhysicalProjectTree projectTree, ICreateFileFromTemplateService templateFileCreationService, ProjectProperties projectProperties)
            : base("App.config", projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
            _properties = projectProperties;
        }

        protected override async Task CreateFileAsync(string path)
        {
            if (await IsNetCore5OrHigherAsync())
            {
                await _templateFileCreationService.CreateFileAsync("AppConfigurationInternalNetCore.zip", path);
            }
            else
            {
                await _templateFileCreationService.CreateFileAsync("AppConfigurationInternal.zip", path);
            }
        }

        /// <summary>
        /// Looks at the ConfigurationGeneral properties to determine if this is targeting .NET 5 or higher.
        /// </summary>
        /// <returns>True if the version found in the TargetFrameworkMoniker is corresponding to .NET 5 or higher; otherwise, false.</returns>
        private async Task<bool> IsNetCore5OrHigherAsync()
        {
            ConfigurationGeneral general = await _properties.GetConfigurationGeneralPropertiesAsync();
            string? targetFrameworkVersion = (string?)await general.TargetFrameworkVersion.GetValueAsync();

            if (string.IsNullOrEmpty(targetFrameworkVersion))
                return false;

            // v6.0 <- remove letter from string.
            targetFrameworkVersion = targetFrameworkVersion.Substring(targetFrameworkVersion.LastIndexOf('v') + 1);
            float version = float.Parse(targetFrameworkVersion);

            if (version >= 5)
                return true;

            return false;
        }
    }
}
