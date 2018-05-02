// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AppManifest)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
    internal class AppManifestSpecialFileProvider : AbstractSpecialFileProvider
    {
        private readonly ProjectProperties _projectProperties;
        private const string NoManifestValue = "NoManifest";
        private const string DefaultManifestValue = "DefaultManifest";

        [ImportingConstructor]
        public AppManifestSpecialFileProvider(IPhysicalProjectTree projectTree,
                                              [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
                                              [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
                                              IFileSystem fileSystem,
                                              ISpecialFilesManager specialFilesManager,
                                              ProjectProperties projectProperties)
            : base(projectTree, sourceItemsProvider, templateFileCreationService, fileSystem, specialFilesManager)
        {
            _projectProperties = projectProperties;
        }

        protected override string Name => "app.manifest";

        protected override string TemplateName => "AppManifestInternal.zip";

        protected override async Task<IProjectTree> FindFileAsync(string specialFileName)
        {
            // If the ApplicationManifest property is defined then we should just use that - otherwise fall back to the default logic to find app.manifest.
            ConfigurationGeneralBrowseObject configurationGeneral = await _projectProperties.GetConfigurationGeneralBrowseObjectPropertiesAsync().ConfigureAwait(false);
            string appManifestProperty = await configurationGeneral.ApplicationManifest.GetEvaluatedValueAtEndAsync().ConfigureAwait(false) as string;

            if (!string.IsNullOrEmpty(appManifestProperty) &&
                !appManifestProperty.Equals(DefaultManifestValue, StringComparison.InvariantCultureIgnoreCase) &&
                !appManifestProperty.Equals(NoManifestValue, StringComparison.InvariantCultureIgnoreCase))
            {
                return _projectTree.TreeProvider.FindByPath(_projectTree.CurrentTree, appManifestProperty);
            }

            return await base.FindFileAsync(specialFileName).ConfigureAwait(false);
        }
    }
}
