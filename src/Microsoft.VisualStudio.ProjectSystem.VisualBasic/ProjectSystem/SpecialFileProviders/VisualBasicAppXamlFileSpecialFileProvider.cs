// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AppXaml)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicAppXamlFileSpecialFileProvider : AbstractFindByItemTypeSpecialFileProvider
    {
        [ImportingConstructor]
        public VisualBasicAppXamlFileSpecialFileProvider(
            IPhysicalProjectTree projectTree,
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
            [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
            IFileSystem fileSystem)
            : base(projectTree, sourceItemsProvider, templateFileCreationService, fileSystem)
        {
        }

        protected override string ItemType => ApplicationDefinition.SchemaName;

        protected override string Name => "Application.xaml";

        protected override string TemplateName => "InternalWPFApplicationDefinition.zip";
    }
}
