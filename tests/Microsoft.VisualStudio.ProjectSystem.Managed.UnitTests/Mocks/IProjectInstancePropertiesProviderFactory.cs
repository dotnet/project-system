// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Execution;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectInstancePropertiesProviderFactory
    {
        public static IProjectInstancePropertiesProvider Create()
            => Mock.Of<IProjectInstancePropertiesProvider>();

        public static IProjectInstancePropertiesProvider ImplementsGetItemTypeProperties(IProjectProperties? projectProperties = null)
        {
            var mock = new Mock<IProjectInstancePropertiesProvider>();

            mock.Setup(d => d.GetItemTypeProperties(It.IsAny<ProjectInstance>(), It.IsAny<string>()))
                .Returns(() => projectProperties ?? Mock.Of<IProjectProperties>());

            return mock.Object;
        }

        public static IProjectInstancePropertiesProvider ImplementsGetCommonProperties(IProjectProperties? projectProperties = null)
        {
            var mock = new Mock<IProjectInstancePropertiesProvider>();

            mock.Setup(d => d.GetCommonProperties(It.IsAny<ProjectInstance>()))
                .Returns(() => projectProperties ?? Mock.Of<IProjectProperties>());

            return mock.Object;
        }
    }
}
