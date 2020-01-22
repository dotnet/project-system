// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

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
