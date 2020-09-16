// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class PropertiesAvailableStatusFactory
    {
        public static IUIEditorMetadataPropertiesAvailableStatus CreateUIEditorMetadataAvailableStatus(
            bool includeName,
            bool includeValue)
        {
            var mock = new Mock<IUIEditorMetadataPropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName);
            mock.SetupGet(m => m.Value).Returns(includeValue);

            return mock.Object;
        }

        public static ICategoryPropertiesAvailableStatus CreateCategoryPropertiesAvailableStatus(
            bool includeDisplayName, 
            bool includeName, 
            bool includeOrder)
        {
            var mock = new Mock<ICategoryPropertiesAvailableStatus>();

            mock.SetupGet(m => m.DisplayName).Returns(includeDisplayName);
            mock.SetupGet(m => m.Name).Returns(includeName);
            mock.SetupGet(m => m.Order).Returns(includeOrder);

            return mock.Object;
        }

        public static IConfigurationDimensionPropertiesAvailableStatus CreateConfigurationDimensionAvailableStatus(
            bool includeName,
            bool includeValue)
        {
            var mock = new Mock<IConfigurationDimensionPropertiesAvailableStatus>();

            mock.SetupGet(m => m.Name).Returns(includeName);
            mock.SetupGet(m => m.Value).Returns(includeValue);

            return mock.Object;
        }
    }
}
