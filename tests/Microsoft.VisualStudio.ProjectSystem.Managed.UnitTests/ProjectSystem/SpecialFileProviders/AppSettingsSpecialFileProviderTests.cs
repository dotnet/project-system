// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    public class AppSettingsSpecialFileProviderTests : AbstractFindByNameUnderAppDesignerSpecialFileProviderTestBase
    {
        public AppSettingsSpecialFileProviderTests()
            : base("Settings.settings")
        {
        }

        internal override AbstractFindByNameUnderAppDesignerSpecialFileProvider CreateInstance(ISpecialFilesManager specialFilesManager, IPhysicalProjectTree projectTree)
        {
            return CreateInstanceWithOverrideCreateFileAsync<AppSettingsSpecialFileProvider>(specialFilesManager, projectTree);
        }
    }
}
