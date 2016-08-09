// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class ConfiguredProjectFactory
    {
        public static ConfiguredProject Create(IProjectCapabilitiesScope capabilities = null)
        {
            var mock = new Mock<ConfiguredProject>();
            mock.Setup(c => c.Capabilities).Returns(capabilities);
            return mock.Object;
        }
    }
}
