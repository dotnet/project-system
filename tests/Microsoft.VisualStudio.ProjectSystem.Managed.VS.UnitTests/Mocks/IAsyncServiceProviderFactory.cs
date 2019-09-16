// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IAsyncServiceProviderFactory
    {
        public static IAsyncServiceProvider Create()
        {
            return Mock.Of<IAsyncServiceProvider>();
        }

        public static IAsyncServiceProvider ImplementGetServiceAsync(Func<Type, object?> action)
        {
            var mock = new Mock<IAsyncServiceProvider>();

            mock.Setup(s => s.GetServiceAsync(It.IsAny<Type>()))
                             .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
