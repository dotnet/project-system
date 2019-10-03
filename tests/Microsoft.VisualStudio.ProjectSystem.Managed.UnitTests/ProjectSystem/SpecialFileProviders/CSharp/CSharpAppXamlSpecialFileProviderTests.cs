// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders.CSharp
{
    public class CSharpAppXamlSpecialFileProviderTests : AbstractAppXamlSpecialFileProviderTestBase
    {
        public CSharpAppXamlSpecialFileProviderTests()
            : base("App.xaml")
        {
        }

        internal override AbstractFindByNameSpecialFileProvider CreateInstance(IPhysicalProjectTree projectTree)
        {
            return CreateInstanceWithOverrideCreateFileAsync<CSharpAppXamlSpecialFileProvider>(projectTree);
        }
    }
}
