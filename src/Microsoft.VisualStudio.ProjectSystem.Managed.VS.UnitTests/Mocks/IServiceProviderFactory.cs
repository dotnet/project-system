// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

namespace Microsoft.VisualStudio
{
    internal static class IServiceProviderFactory
    {
        public static IServiceProvider Create() => Mock.Of<IServiceProvider>();

        public static IServiceProvider Create(Type type, object instance)
        {
            return ImplementGetService(t => {

                if (t == type)
                    return instance;

                return null;
            });
        }

        public static IServiceProvider ImplementGetService(Func<Type, object> func)
        {
            var mock = new Mock<IServiceProvider>();
            mock.Setup(sp => sp.GetService(It.IsAny<Type>()))
                .Returns(func);

            return mock.Object;
        }
    }
}
