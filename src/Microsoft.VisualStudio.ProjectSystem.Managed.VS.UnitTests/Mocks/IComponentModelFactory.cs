// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ComponentModelHost
{
    internal static class IComponentModelFactory
    {
        public static IComponentModel ImplementGetService<T>(T o)
            where T : class
        {
            var mock = new Mock<IComponentModel>();
            mock.Setup(c => c.GetService<T>()).Returns(o);
            return mock.Object;
        }
    }
}
