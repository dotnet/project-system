// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    public class AssemblyResourcesSpecialFileProviderTests : AbstractFindByNameUnderAppDesignerSpecialFileProviderTestBase
    {
        public AssemblyResourcesSpecialFileProviderTests()
            : base("Resources.resx")
        {
        }

        internal override AbstractFindByNameUnderAppDesignerSpecialFileProvider CreateInstance(ISpecialFilesManager specialFilesManager, IPhysicalProjectTree projectTree)
        {
            return CreateInstanceWithOverrideCreateFileAsync<AssemblyResourcesSpecialFileProvider>(specialFilesManager, projectTree);
        }
    }
}
