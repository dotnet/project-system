// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Execution;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

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
