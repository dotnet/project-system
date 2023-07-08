// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectPropertiesProviderFactory
    {
        public static IProjectPropertiesProvider Create(IProjectProperties? props = null, IProjectProperties? commonProps = null)
        {
            var mock = new Mock<IProjectPropertiesProvider>();

            if (props is not null)
            {
                mock.Setup(t => t.GetProperties(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                                .Returns(props);
            }

            if (commonProps is not null)
            {
                mock.Setup(t => t.GetCommonProperties()).Returns(commonProps);
            }

            return mock.Object;
        }
    }
}
