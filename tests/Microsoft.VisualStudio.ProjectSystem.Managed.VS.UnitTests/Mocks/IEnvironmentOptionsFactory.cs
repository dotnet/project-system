// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IEnvironmentOptionsFactory
    {
        public static IEnvironmentOptions Create()
        {
            return Mock.Of<IEnvironmentOptions>();
        }

        public static IEnvironmentOptions Implement<T>(Func<string, string, string, T, T> environmentOptionsValue)
        {
            var mock = new Mock<IEnvironmentOptions>();

            mock.Setup(h => h.GetOption(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<T>()))
                .Returns(environmentOptionsValue);

            return mock.Object;
        }
    }
}
