// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders.VisualBasic
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the WPF Application Definition,
    ///     typically called "Application.xaml" in Visual Basic projects.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AppXaml)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicAppXamlSpecialFileProvider : AbstractAppXamlSpecialFileProvider
    {
        private readonly ICreateFileFromTemplateService _templateFileCreationService;

        [ImportingConstructor]
        public VisualBasicAppXamlSpecialFileProvider(IPhysicalProjectTree projectTree, ICreateFileFromTemplateService templateFileCreationService)
            : base("Application.xaml", projectTree)
        {
            _templateFileCreationService = templateFileCreationService;
        }

        protected override Task CreateFileAsync(string path)
        {
            return _templateFileCreationService.CreateFileAsync("InternalWPFApplicationDefinition.zip", path);
        }
    }
}
