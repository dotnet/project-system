// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AssemblyInfo)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpAssemblyInfoSpecialFileProvider : AbstractSpecialFileProvider
    {
        [ImportingConstructor]
        public CSharpAssemblyInfoSpecialFileProvider(IPhysicalProjectTree projectTree,
                                                     [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
                                                     [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
                                                     IFileSystem fileSystem,
                                                     ISpecialFilesManager specialFilesManager,
                                                     UnconfiguredProject unconfiguredProject)
            : base(projectTree, sourceItemsProvider, templateFileCreationService, fileSystem, specialFilesManager, unconfiguredProject)
        {
        }

        protected override string Name => "AssemblyInfo.cs";

        protected override string TemplateName => "AssemblyInfoInternal.zip";
    }
}
