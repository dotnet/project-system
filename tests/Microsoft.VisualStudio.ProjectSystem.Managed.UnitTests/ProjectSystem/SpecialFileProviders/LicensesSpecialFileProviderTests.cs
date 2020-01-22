// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    public class LicensesSpecialFileProviderTests : AbstractFindByNameUnderAppDesignerSpecialFileProviderTestBase
    {
        public LicensesSpecialFileProviderTests()
            : base("licenses.licx")
        {
        }

        internal override AbstractFindByNameUnderAppDesignerSpecialFileProvider CreateInstance(ISpecialFilesManager specialFilesManager, IPhysicalProjectTree projectTree)
        {
            return new LicensesSpecialFileProvider(specialFilesManager, projectTree);
        }
    }
}
