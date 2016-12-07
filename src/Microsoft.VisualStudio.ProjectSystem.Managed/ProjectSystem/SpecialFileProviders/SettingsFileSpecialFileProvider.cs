// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using static System.Diagnostics.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AppSettings)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class SettingsFileSpecialFileProvider : AbstractSpecialFileProvider
    {
        [ImportingConstructor]
        public SettingsFileSpecialFileProvider([Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)] IProjectTreeService projectTreeService,
                                               [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
                                               [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
                                               IFileSystem fileSystem) 
            : base(projectTreeService, sourceItemsProvider, templateFileCreationService, fileSystem)
        {
        }

        protected override string Name => "Settings.settings";

        protected override string TemplateName => "SettingsInternal.zip";
    }
}
