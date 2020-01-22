// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    public class AppManifestSpecialFileProviderTests : AbstractFindByNameUnderAppDesignerSpecialFileProviderTestBase
    {
        public AppManifestSpecialFileProviderTests()
            : base("app.manifest")
        {
        }

        internal override AbstractFindByNameUnderAppDesignerSpecialFileProvider CreateInstance(ISpecialFilesManager specialFilesManager, IPhysicalProjectTree projectTree)
        {
            var properties = CreateProperties("NoManifest");

            return CreateInstanceWithOverrideCreateFileAsync<AppManifestSpecialFileProvider>(specialFilesManager, projectTree, properties);
        }

        private ProjectProperties CreateProperties(string appManifestPropertyValue)
        {
            return ProjectPropertiesFactory.Create(UnconfiguredProjectFactory.Create(),
                new PropertyPageData(
                    ConfigurationGeneralBrowseObject.SchemaName,
                    ConfigurationGeneralBrowseObject.ApplicationManifestProperty,
                    appManifestPropertyValue));
        }
    }
}
