// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AssemblyInfo)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpAssemblyInfoSpecialFileProvider : AbstractSpecialFileProvider
    {
        [ImportingConstructor]
        public CSharpAssemblyInfoSpecialFileProvider([Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)] IProjectTreeService projectTreeService,
                                               [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
                                               [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
                                               IFileSystem fileSystem) :
            base(projectTreeService, sourceItemsProvider, templateFileCreationService, fileSystem)
        {
        }

        protected override string GetFileNameOfSpecialFile(SpecialFiles fileId)
        {
            return "AssemblyInfo.cs";
        }

        protected override string GetTemplateForSpecialFile(SpecialFiles fileId)
        {
            return "AssemblyInfoInternal.zip";
        }
    }
}
