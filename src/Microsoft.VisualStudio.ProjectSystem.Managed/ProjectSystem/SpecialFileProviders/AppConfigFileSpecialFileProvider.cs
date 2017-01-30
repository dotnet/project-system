// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AppConfig)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class AppConfigFileSpecialFileProvider : AbstractSpecialFileProvider
    {
        [ImportingConstructor]
        public AppConfigFileSpecialFileProvider(IPhysicalProjectTree projectTree,
                                                [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
                                                [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
                                                IFileSystem fileSystem,
                                                ISpecialFilesManager specialFilesManager) 
            : base(projectTree, sourceItemsProvider, templateFileCreationService, fileSystem, specialFilesManager)
        {
        }

        protected override string Name => "App.config";

        protected override string TemplateName => "AppConfigurationInternal.zip";

        protected override bool CreatedByDefaultUnderAppDesignerFolder => false;
    }
}
