// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
