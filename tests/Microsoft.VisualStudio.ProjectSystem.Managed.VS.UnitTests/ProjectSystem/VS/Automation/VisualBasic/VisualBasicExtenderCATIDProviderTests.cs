// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Properties;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.VisualBasic
{
    public class VisualBasicExtenderCATIDProviderTests
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
        [InlineData(ExtenderCATIDType.HierarchyBrowseObject,              PrjBrowseObjectCATID.prjCATIDVBProjectBrowseObject)]
        [InlineData(ExtenderCATIDType.HierarchyConfigurationBrowseObject, PrjBrowseObjectCATID.prjCATIDVBProjectConfigBrowseObject)]
        [InlineData(ExtenderCATIDType.AutomationReference,                PrjBrowseObjectCATID.prjCATIDVBReferenceBrowseObject)]
        [InlineData(ExtenderCATIDType.ReferenceBrowseObject,              PrjBrowseObjectCATID.prjCATIDVBReferenceBrowseObject)]
        [InlineData(ExtenderCATIDType.ProjectBrowseObject,                PrjBrowseObjectCATID.prjCATIDVBProjectBrowseObject)]
        [InlineData(ExtenderCATIDType.ProjectConfigurationBrowseObject,   PrjBrowseObjectCATID.prjCATIDVBProjectConfigBrowseObject)]
        [InlineData(ExtenderCATIDType.FileBrowseObject,                   PrjBrowseObjectCATID.prjCATIDVBFileBrowseObject)]
        [InlineData(ExtenderCATIDType.AutomationFolderProperties,         PrjBrowseObjectCATID.prjCATIDVBFolderBrowseObject)]
        [InlineData(ExtenderCATIDType.FolderBrowseObject,                 PrjBrowseObjectCATID.prjCATIDVBFolderBrowseObject)]
        [InlineData(ExtenderCATIDType.ConfigurationBrowseObject,          PrjBrowseObjectCATID.prjCATIDVBConfig)]
        public void GetExtenderCATID_ReturnsCorrectCadId(ExtenderCATIDType input, string expected)
        {
            var provider = CreateInstance();

            var result = provider.GetExtenderCATID(input, treeNode: null);

            Assert.Equal(expected, result);
        }

        private static VisualBasicExtenderCATIDProvider CreateInstance()
        {
            return new VisualBasicExtenderCATIDProvider();
        }
    }
}
