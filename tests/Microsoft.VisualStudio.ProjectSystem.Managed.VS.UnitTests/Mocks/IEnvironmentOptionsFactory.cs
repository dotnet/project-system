// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

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
