// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders.VisualBasic
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the default 'AssemblyInfo.vb' file; 
    ///     which contains attributes for assembly versioning, COM exposure, and other assembly-level 
    ///     directives and is typically found under the 'AppDesigner' folder.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AssemblyInfo)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicAssemblyInfoSpecialFileProvider : AbstractFindByNameUnderAppDesignerSpecialFileProvider
    {
        private readonly ICreateFileFromTemplateService _templateFileCreationService;

        [ImportingConstructor]
        public VisualBasicAssemblyInfoSpecialFileProvider(
            ISpecialFilesManager specialFilesManager,
            IPhysicalProjectTree projectTree,
            ICreateFileFromTemplateService templateFileCreationService)
            : base("AssemblyInfo.vb", specialFilesManager, projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
        }

        protected override Task CreateFileCoreAsync(string path)
        {
            return _templateFileCreationService.CreateFileAsync("AssemblyInfoInternal.zip", path);
        }
    }
}
