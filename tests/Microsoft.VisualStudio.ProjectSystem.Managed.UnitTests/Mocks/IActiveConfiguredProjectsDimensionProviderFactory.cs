// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    internal static class IActiveConfiguredProjectsDimensionProviderFactory
    {
        public static IActiveConfiguredProjectsDimensionProvider ImplementDimensionName(string value)
        {
            var mock = new Mock<IActiveConfiguredProjectsDimensionProvider>();
            mock.SetupGet(t => t.DimensionName)
                .Returns(value);

            return mock.Object;
        }
    }
}
