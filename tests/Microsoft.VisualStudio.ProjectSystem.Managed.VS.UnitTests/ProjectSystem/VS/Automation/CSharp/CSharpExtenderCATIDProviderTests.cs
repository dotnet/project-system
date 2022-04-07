// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Properties;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.CSharp
{
    public class CSharpExtenderCATIDProviderTests
    {
        [Fact]
        public void GetExtenderCATID_CatIdsUnknown_ReturnsNull()
        {
            var provider = CreateInstance();

            var result = provider.GetExtenderCATID(ExtenderCATIDType.Unknown, treeNode: null);

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

            var result = provider.GetExtenderCATID(input, treeNode: null);

            Assert.Equal(expected, result);
        }

        private static CSharpExtenderCATIDProvider CreateInstance()
        {
            return new CSharpExtenderCATIDProvider();
        }
    }
}
