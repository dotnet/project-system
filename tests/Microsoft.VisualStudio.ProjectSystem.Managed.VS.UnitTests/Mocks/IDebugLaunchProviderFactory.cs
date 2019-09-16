// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public static class IDebugLaunchProviderFactory
    {
        public static IDebugLaunchProvider ImplementCanLaunchAsync(Func<bool> action)
        {
            var mock = new Mock<IDebugLaunchProvider>();

            mock.Setup(d => d.CanLaunchAsync(It.IsAny<DebugLaunchOptions>()))
                                .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
