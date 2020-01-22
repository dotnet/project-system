// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    public class AppConfigSpecialFileProviderTests : AbstractFindByNameSpecialFileProviderTestBase
    {
        public AppConfigSpecialFileProviderTests()
            : base("App.config")
        {
        }

        internal override AbstractFindByNameSpecialFileProvider CreateInstance(IPhysicalProjectTree projectTree)
        {
            return CreateInstanceWithOverrideCreateFileAsync<AppConfigSpecialFileProvider>(projectTree, (ICreateFileFromTemplateService)null!);
        }
    }
}
