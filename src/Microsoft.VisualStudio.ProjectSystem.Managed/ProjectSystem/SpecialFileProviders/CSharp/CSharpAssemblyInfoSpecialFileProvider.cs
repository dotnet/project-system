// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders.CSharp
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the default 'AssemblyInfo.cs' file;
    ///     which contains attributes for assembly versioning, COM exposure, and other assembly-level
    ///     directives and typically found under the 'AppDesigner' folder.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AssemblyInfo)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpAssemblyInfoSpecialFileProvider : AbstractFindByNameUnderAppDesignerSpecialFileProvider
    {
        private readonly ICreateFileFromTemplateService _templateFileCreationService;

        [ImportingConstructor]
        public CSharpAssemblyInfoSpecialFileProvider(
            ISpecialFilesManager specialFilesManager,
            IPhysicalProjectTree projectTree,
            ICreateFileFromTemplateService templateFileCreationService)
            : base("AssemblyInfo.cs", specialFilesManager, projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
        }

        protected override Task CreateFileCoreAsync(string path)
        {
            return _templateFileCreationService.CreateFileAsync("AssemblyInfoInternal.zip", path);
        }
    }
}
