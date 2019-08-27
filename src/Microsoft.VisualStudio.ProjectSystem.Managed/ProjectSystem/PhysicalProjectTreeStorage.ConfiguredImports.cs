// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal partial class PhysicalProjectTreeStorage
    {
        [Export]
        internal class ConfiguredImports
        {
            public readonly IFolderManager FolderManager;
            public readonly IProjectItemProvider SourceItemsProvider;

            [ImportingConstructor]
            public ConfiguredImports(IFolderManager folderManager, [Import(ExportContractNames.ProjectItemProviders.SourceFiles)]IProjectItemProvider sourceItemsProvider)
            {
                FolderManager = folderManager;
                SourceItemsProvider = sourceItemsProvider;
            }
        }
    }
}
