﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders.VisualBasic;

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
