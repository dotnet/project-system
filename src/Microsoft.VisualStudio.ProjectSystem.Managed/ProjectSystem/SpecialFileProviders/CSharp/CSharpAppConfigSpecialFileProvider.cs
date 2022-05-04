// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders.CSharp
{
    /// <summary>
    /// Provides a <see cref="ISpecialFileProvider"/> that handles the default 'App.config' file.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AppConfig)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpAppConfigSpecialFileProvider : AbstractFindByNameSpecialFileProvider
    {
        private readonly ICreateFileFromTemplateService _templateFileCreationService;

        [ImportingConstructor]
        public CSharpAppConfigSpecialFileProvider(IPhysicalProjectTree projectTree, ICreateFileFromTemplateService templateFileCreationService)
            : base("App.config", projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
        }

        protected override Task CreateFileAsync(string path)
        {
            return _templateFileCreationService.CreateFileAsync("AppConfigInternal.zip", path);
        }
    }
}
