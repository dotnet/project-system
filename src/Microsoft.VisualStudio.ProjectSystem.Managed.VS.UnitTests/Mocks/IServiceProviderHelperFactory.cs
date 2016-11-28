// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class IServiceProviderHelperFactory
    {
        public static IServiceProviderHelper Create() => Mock.Of<IServiceProviderHelper>();

        public static IServiceProviderHelper ImplementGlobalProvider(IServiceProvider globalProvider)
        {
            var mock = new Mock<IServiceProviderHelper>();
            mock.SetupGet(s => s.GlobalProvider).Returns(globalProvider);
            return mock.Object;
        }
    }
}
