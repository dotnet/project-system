// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ConfiguredProjectFactory
    {
        public static ConfiguredProject Create(IProjectCapabilitiesScope capabilities = null, ProjectConfiguration projectConfiguration = null)
        {
            var mock = new Mock<ConfiguredProject>();
            mock.Setup(c => c.Capabilities).Returns(capabilities);
            mock.Setup(c => c.ProjectConfiguration).Returns(projectConfiguration);
            return mock.Object;
        }
    }
}
