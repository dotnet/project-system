// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders.VisualBasic
{
    public class VisualBasicAssemblyInfoSpecialFileProviderTests : AbstractFindByNameUnderAppDesignerSpecialFileProviderTestBase
    {
        public VisualBasicAssemblyInfoSpecialFileProviderTests()
            : base("AssemblyInfo.vb")
        {
        }

        internal override AbstractFindByNameUnderAppDesignerSpecialFileProvider CreateInstance(ISpecialFilesManager specialFilesManager, IPhysicalProjectTree projectTree)
        {
            return CreateInstanceWithOverrideCreateFileAsync<VisualBasicAssemblyInfoSpecialFileProvider>(specialFilesManager, projectTree);
        }
    }
}
