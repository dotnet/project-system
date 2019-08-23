// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    internal static class IEnvironmentHelperFactory
    {
        public static IEnvironmentHelper ImplementGetEnvironmentVariable(string result)
        {
            var mock = new Mock<IEnvironmentHelper>();

            mock.Setup(s => s.GetEnvironmentVariable(It.IsAny<string>()))
                .Returns(() => result);

            return mock.Object;
        }
    }
}
