// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectCapabilitiesServiceFactory
    {
        public static IProjectCapabilitiesService Create()
        {
            return Mock.Of<IProjectCapabilitiesService>();
        }

        public static IProjectCapabilitiesService ImplementsContains(Func<string, bool> action)
        {
            var mock = new Mock<IProjectCapabilitiesService>();

            mock.Setup(s => s.Contains(It.IsAny<string>()))
                .Returns(action);

            return mock.Object;
        }
    }
}
