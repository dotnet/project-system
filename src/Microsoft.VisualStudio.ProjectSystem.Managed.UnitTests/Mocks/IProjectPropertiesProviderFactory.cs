// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class IProjectPropertiesProviderFactory
    {
        public static IProjectPropertiesProvider Create(IProjectProperties props)
        {
            var mock = new Mock<IProjectPropertiesProvider>();

            mock.Setup(t => t.GetProperties(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(props);

            return mock.Object;
        }
    }
}
