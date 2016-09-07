// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    public class IDebugLaunchProviderFactory
    {
        public static IDebugLaunchProvider ImplementCanLaunchAsync(bool debugs)
        {
            var mock = new Mock<IDebugLaunchProvider>();

            mock.Setup(d => d.CanLaunchAsync(It.IsAny<DebugLaunchOptions>()))
                                .Returns(() => Task.FromResult(debugs));

            return mock.Object;
        }
    }
}
