// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
