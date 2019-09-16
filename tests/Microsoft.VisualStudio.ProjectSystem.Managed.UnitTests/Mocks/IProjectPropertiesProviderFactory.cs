// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectPropertiesProviderFactory
    {
        public static IProjectPropertiesProvider Create(IProjectProperties? props = null, IProjectProperties? commonProps = null)
        {
            var mock = new Mock<IProjectPropertiesProvider>();

            if (props != null)
            {
                mock.Setup(t => t.GetProperties(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                                .Returns(props);
            }

            if (commonProps != null)
            {
                mock.Setup(t => t.GetCommonProperties()).Returns(commonProps);
            }

            return mock.Object;
        }
    }
}
