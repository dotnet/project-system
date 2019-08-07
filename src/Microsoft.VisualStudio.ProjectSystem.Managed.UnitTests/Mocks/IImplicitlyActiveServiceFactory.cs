// Copyright (c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IImplicitlyActiveServiceFactory
    {
        public static IImplicitlyActiveService ImplementActivateAsync(Action action)
        {
            var mock = new Mock<IImplicitlyActiveService>();

            mock.Setup(a => a.ActivateAsync())
                .ReturnsAsync(action);

            return mock.Object;
        }

        public static IImplicitlyActiveService ImplementDeactivateAsync(Action action)
        {
            var mock = new Mock<IImplicitlyActiveService>();

            mock.Setup(a => a.DeactivateAsync())
                .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
