// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ILoadedInHostListenerFactory
    {
        public static ILoadedInHostListener Create()
        {
            return Mock.Of<ILoadedInHostListener>();
        }

        public static ILoadedInHostListener ImplementStartListeningAsync(Action action)
        {
            var mock = new Mock<ILoadedInHostListener>();
            mock.Setup(t => t.StartListeningAsync())
                .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
