// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the default 'Resources.resx' file; 
    ///     which contains localized resources for a project and is typically found under the 'AppDesigner' 
    ///     folder.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AssemblyResource)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class AssemblyResourcesSpecialFileProvider : AbstractFindByNameUnderAppDesignerSpecialFileProvider
    {
        private readonly ICreateFileFromTemplateService _templateFileCreationService;

        [ImportingConstructor]
        public AssemblyResourcesSpecialFileProvider(
            ISpecialFilesManager specialFilesManager,
            IPhysicalProjectTree projectTree,
            ICreateFileFromTemplateService templateFileCreationService)
            : base("Resources.resx", specialFilesManager, projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
        }

        protected override Task CreateFileCoreAsync(string path)
        {
            return _templateFileCreationService.CreateFileAsync("ResourceInternal.zip", path);
        }
    }
}
