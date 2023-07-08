// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    internal static class IEnvironmentHelperFactory
    {
        public static IEnvironmentHelper ImplementGetEnvironmentVariable(string? result)
        {
            var mock = new Mock<IEnvironmentHelper>();

            mock.Setup(s => s.GetEnvironmentVariable(It.IsAny<string>()))
                .Returns(() => result);

            return mock.Object;
        }
    }
}
