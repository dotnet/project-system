// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the default 'App.config' file; 
    ///     which contains .NET Framework directives for assembly binding, compatibility and runtime
    ///     settings.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AppConfig)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class AppConfigSpecialFileProvider : AbstractFindByNameSpecialFileProvider
    {
        private readonly ICreateFileFromTemplateService _templateFileCreationService;

        [ImportingConstructor]
        public AppConfigSpecialFileProvider(IPhysicalProjectTree projectTree, ICreateFileFromTemplateService templateFileCreationService)
            : base("App.config", projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
        }

        protected override Task CreateFileAsync(string path)
        {
            return _templateFileCreationService.CreateFileAsync("AppConfigurationInternal.zip", path);
        }
    }
}
