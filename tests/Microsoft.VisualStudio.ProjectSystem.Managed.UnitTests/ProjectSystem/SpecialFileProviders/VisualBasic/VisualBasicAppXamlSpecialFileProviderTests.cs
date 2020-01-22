// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders.VisualBasic
{
    public class VisualBasicAppXamlSpecialFileProviderTests : AbstractAppXamlSpecialFileProviderTestBase
    {
        public VisualBasicAppXamlSpecialFileProviderTests()
            : base("Application.xaml")
        {
        }

        internal override AbstractFindByNameSpecialFileProvider CreateInstance(IPhysicalProjectTree projectTree)
        {
            return CreateInstanceWithOverrideCreateFileAsync<VisualBasicAppXamlSpecialFileProvider>(projectTree, (ICreateFileFromTemplateService)null!);
        }
    }
}
