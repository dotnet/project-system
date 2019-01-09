// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Properties;

using VSLangProj;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    public class CSharpExtenderCATIDProviderTests
    {
        [Fact]
        public void GetExtenderCATID_CatIdsUnknown_ReturnsNull()
        {
            var provider = CreateInstance();

            var result = provider.GetExtenderCATID(ExtenderCATIDType.Unknown, (IProjectTree)null);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(ExtenderCATIDType.HierarchyExtensionObject,           PrjCATID.prjCATIDProject)]
        [InlineData(ExtenderCATIDType.AutomationProject,                  PrjCATID.prjCATIDProject)]
        [InlineData(ExtenderCATIDType.AutomationProjectItem,              PrjCATID.prjCATIDProjectItem)]
        [InlineData(ExtenderCATIDType.HierarchyBrowseObject,              PrjBrowseObjectCATID.prjCATIDCSharpProjectBrowseObject)]
        [InlineData(ExtenderCATIDType.HierarchyConfigurationBrowseObject, PrjBrowseObjectCATID.prjCATIDCSharpProjectConfigBrowseObject)]
        [InlineData(ExtenderCATIDType.AutomationReference,                PrjBrowseObjectCATID.prjCATIDCSharpReferenceBrowseObject)]
        [InlineData(ExtenderCATIDType.ReferenceBrowseObject,              PrjBrowseObjectCATID.prjCATIDCSharpReferenceBrowseObject)]
        [InlineData(ExtenderCATIDType.ProjectBrowseObject,                PrjBrowseObjectCATID.prjCATIDCSharpProjectBrowseObject)]
        [InlineData(ExtenderCATIDType.ProjectConfigurationBrowseObject,   PrjBrowseObjectCATID.prjCATIDCSharpProjectConfigBrowseObject)]
        [InlineData(ExtenderCATIDType.FileBrowseObject,                   PrjBrowseObjectCATID.prjCATIDCSharpFileBrowseObject)]
        [InlineData(ExtenderCATIDType.AutomationFolderProperties,         PrjBrowseObjectCATID.prjCATIDCSharpFolderBrowseObject)]
        [InlineData(ExtenderCATIDType.FolderBrowseObject,                 PrjBrowseObjectCATID.prjCATIDCSharpFolderBrowseObject)]
        [InlineData(ExtenderCATIDType.ConfigurationBrowseObject,          PrjBrowseObjectCATID.prjCATIDCSharpConfig)]
        public void GetExtenderCATID_ReturnsCorrectCadId(ExtenderCATIDType input, string expected)
        {
            var provider = CreateInstance();

            var result = provider.GetExtenderCATID(input, (IProjectTree)null);

            Assert.Equal(expected, result);
        }

        private static CSharpExtenderCATIDProvider CreateInstance()
        {
            return new CSharpExtenderCATIDProvider();
        }
    }
}
