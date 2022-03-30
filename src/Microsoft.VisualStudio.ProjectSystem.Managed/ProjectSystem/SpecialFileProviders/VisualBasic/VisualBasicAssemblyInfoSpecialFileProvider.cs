// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
