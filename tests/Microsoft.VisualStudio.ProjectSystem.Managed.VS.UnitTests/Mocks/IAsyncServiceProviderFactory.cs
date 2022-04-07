// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
