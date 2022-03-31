// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio
{
    internal static class IServiceProviderFactory
    {
        public static IServiceProvider ImplementGetService(Func<Type, object?> func)
        {
            var mock = new Mock<IServiceProvider>();
            mock.Setup<object?>(sp => sp.GetService(It.IsAny<Type>()))
                .Returns(func);

            return mock.Object;
        }
    }
}
